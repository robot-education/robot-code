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

Okay so node allows everything to be modified after init
As previously noted, this is sussy baka
Arbitrarily taking on children whenever you want makes things weird

OTOH, it helps avoid annoying patterns where you have to do inverted dependencies
Example - if statements need all their children defined in-line (hard) or before hand (also obnoxious)
So, allowing adding is useful?

Example:
```
enum = Enum(...)
pred = Predicate("annotation", [
    EnumAnnotation("ahh", enum),
    # requires enum value to know about enum type
    If(is_value("ahh", enum.AHH), [
        EnumAnnotation("huh", enum)
    ])
    If += Else([
        EnumAnnoation("wat", enum)
    ])
])
```