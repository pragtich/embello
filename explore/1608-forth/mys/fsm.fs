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
  <BUILDS , , align ;

: ;FSM   DOES>                 ( col# adr -- )
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


: >STATE ( xt -- adr) \ When passed the xt of a FSM, gives data addr
  begin 2+ dup h@ $4780 = until 2+ ; \ Search for the blx r0 just before the data

: STATE! ( a xt -- ) \ Set FSM state to a
  >STATE ! ;

\ Would like to do something like the following, but it chokes on line endings
\ CREATE   ,  , ['] ; begin ' 2dup <>  while , repeat drop 

0 CONSTANT >0   3 CONSTANT >3   6 CONSTANT >6    \ these indicate state
1 CONSTANT >1   4 CONSTANT >4   7 CONSTANT >7    \ transitions in tabular
2 CONSTANT >2   5 CONSTANT >5                    \ representations
\ end fsm code 


