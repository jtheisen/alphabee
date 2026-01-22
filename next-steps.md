
# What's next

We need the PageGrid properly implemented.

At the moment, we have a BitField for the "fundamental page size", which
probably should be line-sized.

So we have the first of the following building blocks, the first two of which
are required:

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

This modified data lives in the same address space, but space that is reserved
for the branch.

The simplest way to implement this is to have another accessor that uses one
more page field for a branch, this time of line size again (like the prime
field, but it's a page field with addresses in their leaves).

Access for data, including access for the data of prime and page field nodes,
checks this field for access. Modifications result in a leaf being changes in
one of the upstream page grid fields.

The consequence is that the first change necessarily replicates a bunch of index
pages for one or more of the upstream grid fields, but the upside is that reads
aren't slowed down: When traversing the tree, we either eventually reach the
modified leaf or we find a zero reference, indicating that we need to continue
our search in the upstream field. It's the same amount of pointer chasing either
way.

With this simple method, we do need to copy any modified page though, no matter
the size, since a modified page field leaf address must point to address space
of the the correct size. When we assume only reasonable object sizes, this
should be acceptable. Otherwise, we would need to have another, more complicated
mechanism for modifying sub-pages only.

In any case, the upstream page grid is never actually touched, only the space
reserved for the branch. For merges:

- At first, there will only be fast-foward. Only changes from an upstream
  databases are ever merged, and those perform only fast-forwards from a single
  writer, which work naively. This is sufficient for the cache case. For other
  use cases,
- multiple writers will frequently collide over upstream page grid
  modifications, which are not semantic conflicts. In order to rebase or merge,
  the modifications must be brought in at potentially different locations. There
  are several approaches to this and these need to be explored at a different
  time.

This still leaves the question of what happens when not only existing pages
are modified, but new ones are required. TODO

## The object field

It's not clear if we need this. Objects could simply be stored in pages of the
respective size. There may be some advantage to storing objects of the same type
in pages together to reserve some space for information common to the object,
but it's likely an overcomplication. This text just serves to remind me of this.

If they are implemented, they would use the page grid.

## The hash table

The hash table needs a growable address space. There are two approaches:

- On growth, we allocate a larger page and incrementally copy all entries over
  or
- on growth, we allocate only a second page of the same size and rely on address
  tanslation in the same way we get subpages from positions.

The latter requires a tree traversal and is therefore slower on reads, although
it requires less moving of data. Implementation-wise, the approaches are
similar.

In any case, the hash table also relies on the page grid.

