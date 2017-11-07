\ Finite state machine
\ By Julian Noble
\ Edits for Mecrisp compatibility by Joris Pragt
\ http://computer-programming-forum.com/22-forth/63a16bfb51edc2e9.htm
\ Also see the excellent articles at http://galileo.phys.virginia.edu/classes/551.jvn.fall01/, including
\ http://galileo.phys.virginia.edu/classes/551.jvn.fall01/fsm.html


\ code to create state machines from tabular representations

: ||   ' ,  ' ,  ;            \ add two xt's to data field
: wide   0  ;                 \ aesthetic, initial state = 0
: fsm:   ( width state --)    \ define fsm
    CREATE  , ( state) ,      ( width in double-cells)  ;

: ;fsm   DOES>                ( x col# adr -- x' )

         *  +                 ( x col#+width*state )
         2*  2 +  CELLS       ( x relative offset )

         DUP >R               ( x adr[action] )
         EXECUTE              ( x' )
         R> CELL+             ( x' adr[update] )
         EXECUTE              ( x' state')
         R> !   ;             ( x' )  \ update state

\ set fsm's state, as in:  0 >state fsm-name
\ : >state   POSTPONE defines  ; IMMEDIATE   ( state "fsm-name" --)

\ : state: ( "fsm-name" -- state) \ get fsm's state
\     'dfa                  \ get dfa

0 CONSTANT >0   3 CONSTANT >3   6 CONSTANT >6    \ these indicate state
1 CONSTANT >1   4 CONSTANT >4   7 CONSTANT >7    \ transitions in tabular
2 CONSTANT >2   5 CONSTANT >5                    \ representations
\ end fsm code 

\ Testing

: WITHIN   ( n a b -- f) 2DUP MIN  -ROT  MAX
             ROT TUCK MIN  -ROT MAX  = ;

: DIGIT?  ( n -- f )   [CHAR] 0  [CHAR] 9 WITHIN  ;

: DP?  [CHAR] .  = ;

: MINUS?  [CHAR] -  = ;

: cat->col#   ( n -- n')
         DUP   DIGIT?   1 AND                    \ digit   -> 1
         OVER  MINUS?   2 AND  +                 \ -       -> 2
         SWAP  DP?      3 AND  +                 \ dp      -> 3
   ;                                             \ other   -> 0

4 WIDE FSM: <Fixed.Pt;#>
\ input:  |  other?  |  num?   |  minus?  |   dp?     |
\ state:  ---------------------------------------------
   ( 0 )    || DROP >0  ||  EMIT >1 ||  EMIT >1  ||   EMIT >2
   ( 1 )    || DROP >1  ||  EMIT >1 ||  DROP >1  ||   EMIT >2 
   ( 2 )    || DROP >2  ||  EMIT >2 ||  DROP >2  ||   DROP >2
;fsm

: Getafix   0  ' <Fixed.Pt;#> !
            BEGIN   KEY   DUP   13 <>      WHILE
            DUP   cat->col#  <Fixed.pt;#>   REPEAT ;

