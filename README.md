# Elfenscript

An interpreted language designed for Unity DOTS. 

## Quickstart 

### Types 

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

### Variables 

Declare variables with the `var` keyword.

```
var a = 1;
```

Variable types are inferred by usage. 

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
