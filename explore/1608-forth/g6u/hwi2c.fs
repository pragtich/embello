
forgetram
compiletoram

\ Define pins
[ifndef] SCL  PB6 constant SCL  [then]
[ifndef] SDA  PB7 constant SDA  [then]

$40005400 constant I2C1
$40005800 constant I2C2
     I2C1 $00 + constant I2C1-CR1
     I2C1 $04 + constant I2C1-CR2
     I2C1 $08 + constant I2C1-OAR1
     I2C1 $0C + constant I2C1-OAR2
     I2C1 $10 + constant I2C1-DR
     I2C1 $14 + constant I2C1-SR1
     I2C1 $18 + constant I2C1-SR2
     I2C1 $1C + constant I2C1-CCR
     I2C1 $20 + constant I2C1-TRISE

3  bit constant APB2-GPIOB-EN
0  bit constant APB2-AFIO-EN
21 bit constant APB1-I2C1-EN
21 bit constant APB1-RST-I2C1

$40021000 constant RCC
     RCC $00 + constant RCC-CR
     RCC $04 + constant RCC-CFGR
     RCC $10 + constant RCC-APB1RSTR
     RCC $14 + constant RCC-AHBENR
     RCC $18 + constant RCC-APB2ENR
     RCC $1C + constant RCC-APB1ENR

     0 variable i2c.cnt
     0 variable i2c.addr
     
\ Checks I2C1 busy bit
: i2c-busy?   ( -- b) I2C1-SR2 h@ 1 bit and 0<> ;

     
\ Init and reset I2C. Probably overkill. TODO simplify
: i2c-init
  \ Reset I2C1
  APB1-RST-I2C1 RCC-APB1RSTR bis!
  APB1-RST-I2C1 RCC-APB1RSTR bic!

  \ init clocks
  APB2-GPIOB-EN APB2-AFIO-EN or RCC-APB2ENR bis!
  APB1-I2C1-EN                  RCC-APB1ENR bis!
  
  \ init GPIO
  IMODE-FLOAT SCL io-mode!  \ edited: manual says use floating input
  IMODE-FLOAT SDA io-mode!  
  OMODE-AF-OD OMODE-FAST + SCL io-mode!  \ %1101 AF means I2C Using external pullup
  OMODE-AF-OD OMODE-FAST + SDA io-mode!  \ We need to connect external pullup R to SCL and SDA

  \ Reset I2C peripheral
   15 bit I2C1-CR1 hbis!
   15 bit I2C1-CR1 hbic!
   
  
  \ Enable I2C peripheral
  21 bit RCC-APB1ENR bis!  \ set I2C1EN
  1  bit I2C1-CR2 hbis!           \ 2 MHz

  \ Configure clock control registers?!

  10 I2C1-CCR h!  \ lets start slow 100 kHz?
  3  I2C1-TRISE h!         \ 2+1 for 1000ns SCL

  0  bit I2C1-CR1 hbis!    \ Enable bit
  10 bit I2C1-CR1 hbis!    \ ACK enable

  \ Wait for bus to initialize
  begin i2c-busy? 0= until
;



\ debugging
: i2c? cr I2C1-CR1 h@ hex. I2C1-CR2 h@ hex. I2C1-SR1 h@ hex. I2C1-SR2 h@ hex. ;

\ Low level register setting and checking
: i2c-DR!     ( c -- )  I2C1-DR c! ;                 \ Writes data register
: i2c-DR@     (  -- c ) I2C1-DR c@ ;                 \ Writes data register
: i2c-start!  ( -- )    8 bit I2C1-CR1 hbis! ; 
: i2c-stop!   ( -- )    9 bit I2C1-CR1 hbis! ;
: i2c-AF-0 ( -- )  10 bit I2C1-SR1 hbic! ;      \ Clars AF flag
: i2c-START-0 ( -- )   8 bit I2C1-CR1 hbic! ;      \ Clears START condition
: i2c-SR1-flag? ( u -- ) I2C1-SR1 hbit@ ; 
: i2c-ACK-1 ( -- ) 10 bit I2C1-CR1 hbis! ;
: i2c-ACK-0 ( -- ) 10 bit I2C1-CR1 hbic! ;
: i2c-POS-1 ( -- ) 11 bit I2C1-CR1 hbis! ;
: i2c-POS-0 ( -- ) 11 bit I2C1-CR1 hbic! ;

\ Low level status checking
: i2c-sb?  ( -- b)   0  bit i2c-SR1-flag? ;      \ Gets start bit flag
: i2c-nak? ( -- b)   10 bit i2c-SR1-flag? ;      \ Gets AF bit flag
: i2c-TxE? ( -- b)   7  bit i2c-SR1-flag? ;      \ TX register empty
: i2c-ADDR? ( -- b)  1  bit i2c-SR1-flag? ;      \ ADDR bit
: i2c-MSL? ( -- b)   0  bit I2C1-SR2 hbit@ ;      \ MSL bit

: i2c-SR1-wait ( u -- ) begin dup i2c-SR1-flag?         until drop ; \ Waits until SR1 meets bit mask
: i2c-SR1-!wait ( u -- ) begin dup i2c-SR1-flag? negate until drop ;

0  bit constant i2c-SR1-SB
1  bit constant i2c-SR1-ADDR
2  bit constant i2c-SR1-BTF
6  bit constant i2c-SR1-RxNE
7  bit constant i2c-SR1-TxE
10 bit constant i2c-SR1-AF

\ Medium level actions, no or limited status checking

: i2c-start ( -- ) \ set start bit and wait for start condition
  i2c-start! i2c-SR1-SB i2c-SR1-wait i2c-START-0 ; \ begin i2c-sb?    until ;

: i2c-stop  ( -- )  i2c-stop! begin i2c-MSL? negate until ; \ stop and wait

: i2c-probe ( c -- nak ) \ Sets address and waits for ACK or NAK
  i2c-start
  shl i2c-DR! \ Send address (low bit zero)
  i2c-SR1-AF i2c-SR1-ADDR or i2c-SR1-wait \ Wait for address sent
  i2c-nak?    \ Put AE on stack (NAK)
  i2c-AF-0
  i2c-stop
;

\ STM32 EV Events

: i2c-EV5   i2c-SR1-SB   i2c-SR1-wait ;
: i2c-EV6   i2c-SR1-ADDR i2c-SR1-AF or
	                 i2c-SR1-wait
	    I2C1-SR2 h@ drop ; \ Wait for address sent or AF
: i2c-EV8_1 i2c-SR1-TxE  i2c-SR1-wait ;
: i2c-EV7   i2c-SR1-RxNE i2c-SR1-wait ;

\ Compatibility layer

: i2c-addr ( u --) \ Start a new transaction and send address in write mode
  i2c-start
  shl dup i2c.addr !
  i2c-EV5

  i2c-DR!                   \ Sends address (write mode)
  i2c-EV6       		\ wait for completion of addressing or AF
;

: i2c-xfer ( u -- nak) \ prepares for an nbyte reply
    dup i2c.cnt !
    dup if     \ cnt >  0           \ Restart after transmission in read mode  
      	
      dup 3 = if
      else
	dup 1 = if
	else
	  dup 2 = if
	    i2c-start  \ set start bit,  wait for start condition
            
	    i2c.addr @ 1 or \ Send address with read bit
	    i2c-DR!
	    
	    i2c-POS-1 i2c-ACK-1
	    
	    i2c-EV6 I2C1-SR2 @ drop \ wait for ADDR and clear
	    i2c-ACK-0
	    i2c-SR1-BTF i2c-SR1-wait \ wait for BTF
	    i2c-stop!                \ set stop without waiting
	  else

	    i2c-start  \ set start bit,  wait for start condition
            
	    i2c.addr @ 1 or \ Send address with read bit
	    i2c-DR!
	    i2c-EV6    \ wait until ready to read
	    \ i2c-SR1-ADDR i2c-SR1-wait

	  then
	then
      then
      drop
      
    else   \ cnt == 0, compatibility equivalent to i2c-probe
      drop
      i2c-nak? i2c-AF-0 i2c-stop
    then
  ;

: >i2c  ( u -- ) \ Sends a byte over USB. Use after i2c-addr
  i2c-EV8_1
  i2c-DR!
;

: i2c>
    i2c-EV7    \ wait until data received
    i2c-DR@
    -1 i2c.cnt +!
    i2c.cnt @ 0= if i2c-POS-0 i2c-ACK-1 then
;

: i2c>h
    i2c> i2c> 8 lshift or
;


\ High level transactions

: i2c. ( -- )  \ scan and report all I2C devices on the bus
  128 0 do
    cr i h.2 ." :"
    16 0 do  space i j +
      dup $08 < over $77 > or if drop 2 spaces else
        dup i2c-probe if drop ." --" else h.2 then
      then	
    loop
  16 +loop ;


\ testing

i2c-init

\ No device
\ i2c-init i2c-start $39 i2c-probe i2c? 
\ 00000401 00000002 00000400 00000003  ok.
\ AE MSL BUSY
\ 
\ Device
\ i2c-init i2c-start $40 i2c-probe i2c? 
\ 00000401 00000002 00000082 00000007  ok.
\ TxE ADDR  TRA BUSY MSL

\ $40 i2c-addr $E3 >i2c 2 i2c-xfer i2c>h . 

\ Device specific stuff, to be moved to driver eventually

: Si7021-T ;
