# Elfenscript

An interpreted language with static typing designed for game scripting.

### Goals 

#### Language

Imperative paradigm for ease of learning, reading, and typing. Alphabetical keywords rather than symbols. One way to do something, less syntactic sugars.

#### Runtime

As performant as possible for an interpreted language without resorting to JIT (for portability & distribution restrictions. Hello, iOS). Easy for the host application to add external functions. Frictionless data providing and retrieval from the host application

## Quickstart 

### Variables 

Declare variables with the `var` keyword.

```
var a = 1
```

The language has static typings but variable types are inferred by usage. 

### Types

#### Primitives

Primitive type supported:
- `Int` (32-bit)
- `Float` (single-precision 32-bit)
- `Byte` (8 bit)
- `Bool`
- References

Literal values are parsed by these rules:

```
// Numbers without a dot (.) are parsed as integers
var int1 = 1
var int2 = -23

// Numbers with a dot (.) are parsed as floats, even zero
var float1 = 34.2
var float2 = 0.0

// Booleans uses the usual true and false values
var bool1 = true
var bool2 = false
```

All variable declaration requires an initial value. You can use the value type as an alias to the zero-value. 

```
var int = Int       // Same as 0
var float = Float   // Same as 0.0
var bool = Bool     // Same as false
```

##### References 

References are built-in pointers for data that lives in the heap.

A reference can be created with the `create` keyword.

A reference of a variable can be passed using the `ref` keyword

```
function Duplicate(ref Int value) returns nothing
    value = value * 2

var a = 10 
Duplicate(ref a)    // a is now 20 

// Reference can be stored in a variable
var refToA = ref a
Duplicate(refToA)    // a is now 40
```

###### Lifetimes

###### Strings

Strings can only be allocated on the heap.

// Strings are quoted with single-quote character
var str = 'Hello world!'

#### Resource Types

```
var refInt = create 12      // A Ref Int
var value = refInt.Unwrap   // Get a copy of the value wrapped inside

delete refInt               // Destroys refInt, the memory is freed

print refInt.Value          // Error: refInt has been destroyed
```

#### Compound Types

Built-in compound types: 
- Spans `T<n>`: Stack-allocated compile-time fixed-sized array  
- Lists `T[]`: Heap-allocated dynamic-sized array
- Tuple `(T1, T2, ... Tn)`: Stack-allocated fixed group of values 
- Map `[Tk] -> Tv`: Heap-allocated hash map
- Functions `(T1, T2, ... Tn) -> TR`

##### Structs

Structs are user-defined data structures.

```
structure Data
    One Int
    Two Float
    
```

##### Spans

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

Common operators can be used for spans of the same type

```
var a = { 1, 2, 3 }
var b = { 4, 5, 6 }

var add = a + b     // { 5, 7, 9 }
var sub = a - b     // { -3, -3, -3 }
```

Span members can be accessed with the dot accessor operator `.` and their index number. This implies there is no way to access span members programmatically like arrays.

```
var a = { 1, 2, 3 }
var b = a.0 - a.1 + a.2 * a.1
```

##### Lists 

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

##### Maps

##### Tuples

Tuples are anonymous structs, identified by ordered type signature of its members.

```
var 
```

#### Alias

Type can be aliased with the `alias` keyword for convenience.

```
alias Position as Int<2>
alias Name as Byte<32>

structure Character 
    Label Name
    CurrentPosition Position
    
var char1 = Character
    Name = 'One'
    CurrentPosition = {1, 2}
    
var char2 = Character 
    Name = 'Two'
    CurrentPosition = {-4, 10} 

// Returns the delta cell position and distance
function Distance(Character first, Character second) returns (Position, Float) 
    var delta = Math.Abs(first.Position - second.Position)
    var dist = Math.Distance(first.Position, second.Position)
    return (delta, dist)
```

#### Sharing

All variables are allocated exclusively for one process.

In a multithreading environment and as a scripting language, other processes or external code might want to access the same data concurrently.

This can be achieved by marking variables with the `shared` keyword. Shared variables is allocated on a special heap that can be accessed by multiple processes. They are like built-in references wrapped with a mutex.

```
// Global shared variable, not recommended
shared var sharedInt = 10

// sharedInt read-only mutex lock
var a = sharedInt   // This line will read lock the 'sharedInt' and unlock it after assignment is successful
// sharedInt read-only mutex release

// sharedInt read-write mutex lock
sharedInt = sharedInt + 10
// sharedInt read-write mutex release
```

The shared attribute is treated as a data type, hence a function can accept `shared` parameter:

```
function Increment(shared Int x) returns nothing
    x = x + 1
```

### Functions 

Functions can be declared with the `function` keyword: 

```
function Add (Int a, Int b) returns Int
    return a + b
    
var result = Add(1, 2)
```

Functions are first-class members, therefore it's possible to store functions in variables. Functions have the type signature `(<args>) -> <return type>`

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

#### Evaluate 

You can use `evaluate` to write **IIFE**s (Immediately Invoked Function Expression). This is useful to compute variable value. 

```
var value = 10
var absoluteValue = evaluate 
    if value < 0 then 
        return -value
    return value
```

Another example to count the number of odd numbers in a span of numbers:
```
var numbers = { 1, 4, 7, 3, 8, 8, 9, 3, 1, 6 }
var oddCount = evaluate
    var result = 0
    foreach var number in numbers with index i
        if number % 2 == 0 then 
            result = result + 1
    return result
```

### Control Flow 

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

The language supports `while` loops for general-purpose looping and `for` loop for arrays and iterables. 

```
var x = 1
while x < 1000000
    x = x * x

Print(x)
```

`for` loop for arrays:

```
var strings = ["one", "two", "three"]
for str in strings
    Print(str)
```

Add `range` to loop with index

```
var strings = ["one", "two", "three"]
for str, i in range strings
    Print('{i}: {str}')
```

Loop with a specified index range 

```
for i in range 10
    Print(i)
```

```
for i in range 5 - 10 to 10 * 2
    Print(i)    // prints from -5 to 20
```

```
var head = 5
var tail = 10
for i in range head to tail
    Print(i)
```

`break` and `continue` can be used to control loop flows.

```
var i = 0
var evenSum = 0
while true
    if i % 2 == 0
        evenSum = evenSum + i
    else 
        continue

    if i == 50 
        break
    
    i = i + 1

print(evenSum)
```

#### Foreach 

`foreach` is a special keyword to iterate on collections such as spans and lists. 

```
var numbers = {1, 2, 3, 4, 5} 
foreach number, i in numbers 
    numbers[i] = numbers[i] * 2 
    
debug numbers
```

### Modules

All importable scripts requires module declaration at the top of the file. 

```
module MyLibrary

function MyFunction returns Int 
    return 10
```

Use the `use` keyword to import a module 

```
use MyLibrary

log MyLibrary.MyFunction() // prints '10'
```

#### Scope

Modules can be declared inside another module with the `.` in the identifier

```
module MyLibrary.OtherLib

function MyFunction returns Int
    return 20
```

```
use MyLibrary
use MyLibrary.OtherLib

log MyLibrary.MyFunction()  // prints 10
log OtherLib.MyFunction()   // prints 20
```

#### Alias

Module imports can be aliased, this is optional however it is require if there is a module base name conflict.

```
module MyLibrary.Deep.OtherLib

function MyFunction returns Int
    return 30
```

```
use MyLibrary
use MyLibrary.OtherLib
use MyLibrary.Deep.OtherLib     // compile error! conflicting imported module names
```

```
use MyLibrary
use MyLibrary.OtherLib
use MyLibrary.Deep.OtherLib as DeepOtherLib

log MyLibrary.MyFunction()      // prints 10
log OtherLib.MyFunction()       // prints 20
log DeepOtherLib.MyFunction()   // prints 30
```

### Convention

#### Naming 

All types and functions uses `PascalCase` such as `Int`, `Float`, `PlayerInventory`, `CalculateDistance` and so on. 

All variables uses `camelCase`. 

Abbreviations uses capital letters, such as `ID`, `DPS`, etc. 
