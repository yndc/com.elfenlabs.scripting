# Elfenscript

An interpreted language designed for Unity DOTS. 

## Quickstart 

### Variables 

Declare variables with the `var` keyword.

```
var a = 1
```

Variable types are inferred by usage. 

## Types 

### Primitives

Primitive type supported:
- `Int` (32-bit)
- `Float` (single-precision 32-bit)
- `Byte` (8 bit)
- `Bool`
- `Null` for reference types

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
var refInt = create 12      // A Ref Int
var value = refInt.Unwrap   // Get a copy of the value wrapped inside

destroy refInt              // Destroys refInt, the memory is freed

print refInt.Value          // Error: refInt has been destroyed
```

### Compound Types

4 compount types are available to use: 
- Spans `T<n>`: Stack-allocated compile-time fixed-sized array  
- Lists `T[]``: Heap-allocated dynamic-sized array
- Tuple `(T1, T2, ... Tn)`: Stack-allocated fixed group of values 
- Map `[Tk] -> Tv`: Heap-allocated hash map
- Functions `(T1, T2, ... Tn) -> TR`

#### Spans

Spans are fixed-sized arrays allocated on the stack. 

```
var numbers = Int<3>
numbers = { 1, 2, 3 }
```

Spans are passed by value and doesn't need to be destroyed after use. 

```
function Dot (Float<2> vector) returns Float 
    return vector[0] * vector[0] + vector[1] * vector[1]

var vector = { 10.0, 5.0 }
var dot = Dot(vector) 
```

#### Lists 

Lists are dynamically-sized array allocated on the heap. 

```
var numbers = create Int[]  // Create an empty list
numbers.Add(1)
numbers.Add(2)
numbers.Add(3)
var len = numbers.Length()

// Accessing list values 
var first = numbers.First()
var middle = numbers[1]   
var last = numbers.Last()

// Lists need to be destroyed after use to prevent memory leaks 
destroy numbers
```

Lists are reference types, and therefore passed by reference 

```
function AddZero(Int[] array)
    array.Add(0)
    
var arr = create Int[]
arr.Add(1)
AddZero(arr)

Print(arr.Length())     // Prints "2" 
Print(arr.Last())       // Prints "0"

destroy arr
```

#### Maps

#### Tuples

#### Structs

Define a structure with the `structure` keyword.

```
structure Data
    Number Int
    
```

## Functions 

Functions can be declared with the `function keyword: 

```
function Add (Int a, Int b) returns Int
    return a + b
    
var result = Add(1, 2)  // 
```

Functions are first-class citizens, therefore it's possible to store functions in variables. Functions have the type signature `(<args>) -> <return type>`

```
function AddOne (Int x) returns Int   
    return x + 1
    
function TimesTwo (Int x) returns Int 
    return x * 2
    
// Both functions have the same type signature of '(Int) -> Int'
// Therefore they can be added in the same collection
var operations = { AddOne, TimesTwo, TimesTwo }     // The type is ((Int) -> Int)<3>

var number = 1
foreach operation in operations 
    number = operation(number)
    
Print(number)       // Prints 8
```

## Control Flow 

### If Statements

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

### Branch 

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

### Loops 

To create an infinite loop, use the `loop` keyword. `break` and `continue` can be used to break out of loops.

```
var counter = 0
loop 
    Print(counter)
    if counter > 5
        break
    counter = counter + 1 
```

Add a condition to a loop with `while`:

```
var counter = 0
var n = 5
loop while counter <= n 
    Print(counter)
    counter = counter + 1
```

Loop a specific number of times with `loop <Int> times`

```
var n = 5
loop n times with counter
    Print(counter)
```

#### Foreach 

`foreach` is a special keyword to iterate on collections such as spans and lists. 

```
var numbers = {1, 2, 3, 4, 5} 
foreach number in numbers with i 
    numbers[i] = numbers[i] * 2 
    
debug numbers
```
