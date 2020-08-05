grammar ArcaeaFileFormat;

Whitespace:[\p{White_Space}] -> skip;

LParen: '(';
RParen: ')';
LBrack: '[';
RBrack: ']';
LBrace: '{';
RBrace: '}';
Comma: ',';
Semicolon: ':';

fragment ALPHABET: [a-zA-Z];
Word: ALPHABET+;

fragment DIGITSTART: [1-9];
fragment ZERO: '0';
fragment DIGIT: DIGITSTART | ZERO;
fragment DOT: '.';
fragment NEGATIVE: '-';
Int: NEGATIVE? (ZERO | DIGITSTART DIGIT*);
Float: Int DOT DIGIT+;

value: Word | Int | Float;
values: LParen (value (Comma value)+)? RParen;
event: Word? values subevents? segment?;
item: event Semicolon;
subevents : LBrack (event (Comma event)+)? RBrack;
segment: LBrace body RBrace;
body: item*;