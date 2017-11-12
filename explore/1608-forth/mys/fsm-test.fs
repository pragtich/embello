
\ Testing fsm.fs
\ needs fsm.fs

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
