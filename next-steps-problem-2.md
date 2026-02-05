There's another issue with fast-forwards, this time with objects.

We really have the choice of relying on b-tree translations either indefinitely
or copying objects back to their former location when no reader still needs
them.

We can't just lock out everything on upstream merges as obviously readers might
still need the old data at that point.

The first option is easiest to implement and is required by the second option
anyway, so that's an easy thing to concentrate on.

# The indefinite translation

We still need variagates and the respective page fields to implement them.

The leaves for the page field contain only one address to the variagate and a
bit array with on bit per partition in the variagate.

At this point, we have a memory leak though.

# Copying back

To know when objects can be moved back and partitions freed again we need to
record this information at the transaction level. We need some data structure to
know when a snapshot is no longer needed, at which point the copying to the old
objects (which are then guaranteed to be no longer read) can occur.

At that moment, the new locations become the "stale" ones again and must be
enlisted in a similar manner.

# The only alternative

The only alternative is to update references, which is even more complicated and
also requires knowing when those addresses are allowed to be updated - made even
more difficult as they are used in b-trees for translation.
