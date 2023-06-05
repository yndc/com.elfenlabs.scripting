# Elfenscript

An interpreted language designed for Unity DOTS. 

## Quickstart 

### Variables 

Declare variables with the `var` keyword.

```
var a = 1
```

Variable types are inferred by usage. 

### Types 

Primitive type supported:
- `Int` (32-bit)
- `Float` (single-precision 32-bit)
- `Bool`

Literal values are parsed by these rules:

```
// Numbers without a dot (.) are parsed as integers
var int1 = 1
var int2 = -23

// Numbers with a dot (.) are parsed as floats, even zero
var float1 = 34.2
var float2 = 0.0

// Strings are quoted with single-quote character
var str = 'Hello world!'

// Booleans uses the usual true and false values
var bool1 = true
var bool2 = false
```

All variable declaration requires an initial value. You can use the value type as an alias to the zero-value. 

```
var int = Int       // Same as 0
var float = Float   // Same as 0.0
var str = String    // Same as an empty string ("")
var bool = Bool     // Same as false
```

#### Reference Types

A reference type can be created with the `create` keyword.

```
var refInt = create 12   // A Ref Int
var value = refInt.Value // Get a copy of the value

destroy refInt           // Destroys refInt, the memory is freed

print refInt.Value       // Error: refInt has been destroyed

```

#### Structs

Define a structure with the `structure` keyword.

```
structure Data
    Number Int
    
```

### Conditionals 

#### If Statements

As an indented language, this is the syntax for an if expression: 

```
if <condition> then
    <statement>
else 
    if <condition> then 
        statement 
```

There is no `else if` construct, to create that us an `if` inside the `else` block instead. If this might cause too much nesting for your intended flow, either use a `branch` statement instead or restructure your code for early exit.

This is the way to write single-line if expression:

```
if <condition> then <statement>
```

For multiline conditions, this is how to write them:

```
if <condition>
    and <condition>
    and <condition>
then 
    <statement>
    <statement>
    ..
else 
    <statement>
```

#### Branch 

For complex multiway branching, use the `branch` statement:

```
branch 
    <some_very_long_condition>
    and <some_very_long_condition>
    and <some_very_long_condition>
    or <some_very_long_condition>
    then 
        <statement>
        <statement>
        <statement>

    <condition> and <condition>
    then 
        <statement>
        <statement>

    (<condition> and <condition>)
    and (
        <some_very_long_condition>
        or <some_very_long_condition>
    )
    then 
        <statement>
        <statement>
```

The branches are evaluated from top to bottom.

### Collections

#### List 

Lists are dynamic sized and they are allocated on the heap.

```
// Declare an array of int 
var list1 = Int[4]

// Declare an array without predefined size 
var list2 = Int[]
list2.Add 2
list2.Add 5
list2.Add 10

// Lists need to be disposed 
dispose list1 
dispose list2

```

#### Spans 

Spans are like arrays but allocated on the stack, i.e. you don't need to dispose of them. They have a fixed size.

```
var span = Int<4>
span[0] = 5
span[2] = span[0]
```
