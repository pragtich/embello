\ Finite state machine
\ By Julian Noble
\ Edits for Mecrisp compatibility by Joris Pragt
\ https://web.archive.org/web/20011008085910/http://www.jfar.org:80/article001.html
\ 
\ Also see the excellent articles at http://galileo.phys.virginia.edu/classes/551.jvn.fall01/, including
\ http://galileo.phys.virginia.edu/classes/551.jvn.fall01/fsm.html


\ code to create state machines from tabular representations

: WIDE   0 ;

: FSM:   ( width 0 -- )
  CREATE , , align ;

: ;FSM   DOES>                 ( col# adr -- )
\ 	  3 cells -
          dup >R  2@           ( col#  width state )
	  * +                  ( col#+width*state )
          2*  2+  CELLS        ( offset-to-action)
	  R@ +                 ( @action)
          DUP >R               ( offset-to-action)
          @ EXECUTE
          R> CELL+             ( -- ? offset-to-update)
          @ EXECUTE            ( -- ? state')
          R> ! ;               ( ? )       \ update state

: |  ' ,     ;
: || ' , ' , ;

\ Would like to do something like the following, but it chokes on line endings
\ CREATE   ,  , ['] ; begin ' 2dup <>  while , repeat drop 

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
\ input:  ||   other?  ||  num?   ||  minus?  ||   dp?     
\ state:  ---------------------------------------------
   ( 0 )  ||   DROP >0 || EMIT >1 || EMIT >1  || EMIT >2
   ( 1 )  ||   DROP >1 || EMIT >1 || DROP >1  || EMIT >2
   ( 2 )  ||   DROP >2 || EMIT >2 || DROP >2  || DROP >2 ;FSM

: getafix   0  ['] <Fixed.Pt;#> 3 cells + ! 
            BEGIN   KEY   DUP   13 <>      WHILE
            DUP   cat->col#  <Fixed.pt;#>   REPEAT ;


