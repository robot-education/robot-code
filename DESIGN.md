### Design

# TODOs:
Implement builder pattern for enum and annotation
Add child management to `statement` class
Add `add()` method to `statement` which uses generics and returns the entity which was added
Stop overriding `__add__`
Define top level statement class

Move enum conditionals from `EnumAnnotation` to `Enum` and take `parameter_name` as arguments
Add `make_annotation` method to `Enum`?

Have `EnumValue` take `Enum` as a child?
Leave arguments alone for now?
Tweak `EnumValue` parameters to take `str` where possible
Figure out what is and isn't required for `str` - maybe remove `__rstr__`
Make explicit utils class

Tweak predicate and the like to take their arguments at initialization, none of this + nonsense
Or this not adding nonsense either

Declared but not used warnings?