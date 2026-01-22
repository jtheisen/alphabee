
There's a problem with my plans on snapshots.

Readers wish to have a plain, untranslated address space to quickly access data
from simple addresses.

Writers, and readers reading from a branch, need a translation though.

## Full translation

Approach 1.

The naive implementation has the address space always translated, either

- only at the line level or
- at the any page level (which requires copying more than a line on writing).

This works, but is inefficient as the translation tree is quite deep when the
nodes are line-sized, and they need to be line-sized to not make minor changes
wasteful.

The advantage of this naive approach is that it's simple to implement and
changing branches requires only in looking at a different root.

Let's call this the *full translation* approach.

Reads should be faster though.

## Partial translation

We need to find a better aproach where main branch is kept linear and limit
translation to either branches or modified pages.

### Costly fast-forwards

Approach 2.

Changes are applied to the main branch directly, requiring to lock it. This
requires either to translate addresses in the modified data or to do the
modifications directly when building the modifications themselves.

This may be accetable in the cache case, where all modifications come from the
upstream source, severely limiting how much needs merging.

Readers working on the main branch will be fast, but readers on other branches
are still slow, as are writers.

This means that one cannot have readers on snapshots of different age while all
remain efficient.

This is somewhat bad.

### Mark pages as potentially modified on themselves

Approach 3.

We reserve some space on each page to mark them as being modified on some
branch. Readers try to access the page directly first but if it turns out
to have modifications on some branch, we revert to tree traversal.

This can work and is efficient, but it's complicated to implement and
requires the reserved space on each page, reducing flexibility.

The complexity is mainly in figuring out efficiently when the flag should be
reset:

- Instead of a flag one could also use a counter: this makes the implementation
  somewhat easy, but this would increase the amount of space to be reserved.
- Another alternative is to store a transaction count. This has the same
  disadvantage of requiring more space and the additional one that only a linear
  series of commits can use it, but no cleanup would be necessary. This would be
  a fine solution for pure readers who need read from one of the commits of the
  main branch, but writers would have to always traverse the tree.

I'm inclined to think this is one of the better ideas.

### Broad index pages after all

Approach 4a.

We mitigate the issue of tree depth instead, by increasing the size of nodes.

For readers, this is great. Writers, however, need to copy the larger index
pages on each write, creating waste there. Without any locality of reference
beyond the cache line, this waste may be almost prohibitive.

I have my doubts about this approach as well.

Approach 4b.

An optimization one can do in all cases to reduce tree depth is to reduce the
word size for some levels of the tree:

Let's say a cache line of 64 bytes contains byte-pointers to further lines in
the same super-page. This allows 255 further lines to be referenced by each
byte, up to 64 of which are referenced directly by the first line. These
referenced lines then contain the actual addresses, 8 each. That gives us 8 * 64
= 512 actual children, the bit pattern of which fits again in a line.

Since the super-page has 256 - 64 - 1 - 1 space left, modifications have enough
space to entirely rewrite everything if needed.

Reads need to access two lines to traverse over this kind of node, giving a tree
depth of slightly less than log_512 (1 << 31) = 3.5, so slightly less than 7
line accesses per memory access for a reasonably-sized address space.

### B-trees

Approach 5

We store modified addresses in b-trees rather than relying on modified indexes
for downstream content resolution.

Any access is merely looking at this new b-tree and, if the address isn't found,
can make a direct access to the upstream space. For small modifications, the
b-tree is shallow and this lookup will be much faster than a page index lookup.

The leaves of page indexes don't need to be modified either, neither directly
nor as an entry in the b-tree, as the b-tree will already contain all necessary
information to update them on a fast forward.

The entries of those b-trees know the size of the page they represent, and a
writing object access always copies the entire object to a new location.

Page indexes are therefore then only used for allocation.

New allocations from within a transaction can
- either allocate within pre-reserved space or, much simpler though slower,
- use the upstream allocator as a first, more naive implementation.

Nested transactions can re-use upstream b-tree pages and don't need to copy the
entire tree.

So far, this would work fine, leaving only some choice regarding the index page
updates during a fast-foward.

Appraoch 5.0

On fast-forward, only the index pages require updating, during which all other
threads need to be locked out. This is likely a decent choice at first provided
there's another option later:

Approach 5.1

All index pages that require changing a first copied and then modified. The
final change is then reduced to a simple address change as previously planned.

Approach 5.2

Like 5.1, but we allocate some of the changes close to the original ones in the
same super-page where enough space is reserved for at least one such
fast-forward.

Open questions:

- where's the information stored
  - from which an object was copied and
  - where the index page entry was that needs to be updated?
    - in the b-tree?
    - in the new object?

