# Another look

## Another translation level

We introduce another translation mechanism for all objects/pages that are not
translated by b-trees.

This is a simple array (or almost-array) that maps page-ids (possibly now even
32-bit) to the actual addresses.

This design incurs another performance penalty, but it has a big advantage:

On modification, only the new version has to be written. The old one can live on
where it located, no further moves need to be made. Only the b-trees and the
entry in the translation table need changing on snapshot switching.

There are no reference counts in the table as they neither make sense here nor
are they needed, see below.

Writing a change consists of

- allocating new pages/objects,
- writing those pages/objects,
- ensuring the pages have been written,
- writing and ensuring a yet unknown amount of bookkeeping data for the
  transaction.

## The durability and bookkeeping question

This concerns the *global state* outside of branches:

- prime pages (lines)
- further page management
- the translation field

### General requirements

We need to write modifications ahead to ensure atomicity and durability
together.

When required to write ahead, tree-like structures have the advantage that they
can be read from while keeping already persisted version valid.

Achieving this without tree-like structures is tricky and usually involves
writing the respective data twice (as is the case when using write-ahead-logs).

We assume that we have something *like* a wal, in the sense that we write some
bookkeeping first, no necessarily a separate file.

#### Durable writes to the translation field

A commit consists of

- a b-tree relative to the currently canonical version,
- a reverse b-tree.

Committing a switch from commit A to commit B works like this:

- Write in the wal that commit B is now canonical.
- Make reads from commit A use the reverse tree and keep reads from commit B
  using the forward tree as usual. As of now, the respective entries in
  the translation field are not used by any reader.
- Update the translation field entries and sync them.
- Write in the wal that commit B is now canonical and synced.

If a crash occurs before the last step, the last two steps are repeated.

This essentially solves all issues concerning durability of switches if we're
using only some kind of tree for the rest of global bookkeeping.

### B-trees for page management

Let's explore using b-trees for maintaing data about holes in the address space.

This b-tree would be special in the sense that it's global and does not live on
branches. There'd be one for each page size. Since a b-tree can see in their
leaves whether to adjacent pages can be merged, we even have an efficient way to
move two pages of size n to the tree containing the pages of size n * 2.

It would, however, incur a lot more data to write on each transaction.

Also, we likely don't want to use this a prime field replacement as then the
b-tree would need itself for allocation - not sure if that can work.

### Classic fields for page managment

# Questions

## How do we know that a page version is no longer needed?

If all required commits of a branch can be accessed through translation trees
(forward and reverse), then on releasing a commit at the past end can release
the pages that are translated - those will no longer be needed (or else why did
they need translation).

We just need to make sure that branches that the commit itself was indeed no
longer needed - there may be a branch that was branched off of it. This requires
thinking over branch management, but there is no problem that is particular to
freeing pages here.

## How do we guarantee durability for the translation field and the page fields?

See above. This is solved.

## How do reverse trees work, how are they maintained?

This is indeed an issue. The easiest solution is to have b-trees only record
the change relative to the neighbouring commits and we do multiple lookups.

## Do we still use page fields or do we do something with b-trees?

TODO

## Is the prime field special after all?

TODO

## The details of creating a commit vs committing a snapshot switch

TODO
