
# What's next

We need the PageGrid properly implemented.

At the moment, we have a BitField for the "fundamental page size", which
probably should be line-sized.

So we have the first of the following building blocks, the first two of which
are required:

(For the idea to use b-trees instead of fields for page management, see the
section on b-trees.)

## The prime field

This is a bit field required for the fundamental page allocation of a fixed
page-size.

The leaves of the prime field store only bits - all addresses can be calculated
from the bit position.

The prime field can also reserve larger pages simply by not fully descending the
tree.

## The page field

Each page size other than the fundamental one needs a field that records where
the respective pages are.

The branches are just like that of bit fields, but the leaves need to store
addresses.

The page field requests pages from the prime field in an adequate, constant size
larger than the one that it itself allocates.

On deallocation, it tells the prime field that it is this larger size that is
deallocated.

In contrast to the prime field, resolving addresses by position actually
requires traversing the tree, but this is an operation that is only necessary
for

- allocation and deallocation,
- hash tables, if implemented this way, and
- user-defined fields.

Other uses hold addresses of the pages themselves, not their position in a page
field.

## The page grid

The previous two implement the page grid. An accessor can now be written that
allows

- allocation and deallocation of pages of any size the power of 2 and
- getting spans for the data of these pages.

## The virtual page grid

For snapshots, we need a translation mechanism to allow portions of memory to
redirect to modified data.

This modified data lives in the same address space, but space that is allocated
for the branch and therefore exclusive to it.

This will be implemented with a b-tree that records the addresses of all
modified pages and their respective sizes (in one 64 bit integer).

Access for data, not including access for the data of prime and page field
nodes, checks this tree before falling back to direct access. If transactions
are nested, there's still only one b-tree, but upstream tree nodes may be
re-used.

In any case, the upstream page grid layout is is never actually changed and the
non-exclusive addresses never written, only the space reserved for the branch.
For merges, we will limit ourselves to fast-forwards at first.

Fast forwards can be implemented in three steps of improved efficiency:

Approach #0

On fast-forward, only the index pages require updating, during which all other
threads need to be locked out. This is likely a decent choice at first provided
there's another option later:

Approach #1

All index pages that require changing a first copied and then modified. The
final change is then reduced to a simple address change as previously planned.

Approach #2

Like #1, but we allocate some of the changes close to the original ones in the
same super-page where enough space is reserved for at least one such
fast-forward (I hope this is enough to trigger my memory, but it's not important
enough to write up in detail at this point).

Allocations are related as they also need the page indexes, so in the spirit of
approach #0 we go with a simple locking strategy, although it's easy to do
better.

## The object field

It's not clear if we need this. Objects could simply be stored in pages of the
respective size. There may be some advantage to storing objects of the same type
in pages together to reserve some space for information common to the object,
but it's likely an overcomplication. This text just serves to remind me of this.

If they are implemented, they would use the page grid.

## The hash table

Hash tables fill only a special niche when b-trees are already in. It's likely
we don't need this, but here's the plan for if we do:

The hash table needs a growable address space. There are two approaches:

- On growth, we allocate a larger page and incrementally copy all entries over
  or
- on growth, we allocate only a second page of the same size and rely on address
  tanslation in the same way we get subpages from positions.

The latter requires a tree traversal and is therefore slower on reads, although
it requires less moving of data. Implementation-wise, the approaches are
similar.

In any case, the hash table also relies on the page grid.

## The b-tree

B-trees serve multiple purposes:

1. Address translation (for snapshots, etc.)
2. Dictionaries to find object addresses from database keys
3. Page allocation (probably not)
4. Search indexes (user-defined or otherwise)
 
Only the first two are really necessary for the cache use case.

Each b-tree node consists of two pages,

- the index page, an array of index entries and
- the value page, an array of either branch addresses or leaf values.

If the b-tree manages addresses, all index page entries and leaf values have
their lowermost 6 bits used to indicate the address space size they cover.

For leaf values, this is a real page size that is allocated. For index entries,
this is the space the respective subtree covers, rounded up. This is just to
help recognizing early that futher traversal isn't required and direct access is
possible.

### Using B-trees as a replacement for fields

Possible in principle, but we still need different roots for different page
sizes, as we can't efficiently locate space of a specific size in a b-tree.

That also means that we still need to get super-sized pages from some kind of
"prime" and that leaves much of the complexity as it is.

I doubt that this makes much sense