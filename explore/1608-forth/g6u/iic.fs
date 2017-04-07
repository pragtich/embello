
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

: i2c-busy?   ( -- b) I2C1-SR2 h@ 1 bit and 0<> ;

     
\ reset I2C1
: i2c-reset
  APB1-RST-I2C1 RCC-APB1RSTR bis!
  APB1-RST-I2C1 RCC-APB1RSTR bic!

  \ init clocks
  APB2-GPIOB-EN APB2-AFIO-EN or RCC-APB2ENR bis!
  APB1-I2C1-EN                  RCC-APB1ENR bis!

  
  \ init GPIO
  IMODE-FLOAT SCL io-mode!  \ edited: manual says use floating input
  IMODE-FLOAT SDA io-mode!  
  OMODE-AF-OD OMODE-FAST + SCL io-mode!  \ %1101 AF means I2C Using external pullup
  OMODE-AF-OD OMODE-FAST + SDA io-mode!

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

  begin i2c-busy? 0= until
;

i2c-reset


\ Enable peripheral
\ 10 bit 0 bit or I2C1-CR1 hbis!     \ PE, enable device | ACK

: i2c-master? ( -- b) I2C1-SR2 h@ 0 bit and 0<> ;
: i2c-sb? ( -- sb)   I2C1-SR1 h@ 0 bit and 0<> ;
: i2c-addr? ( -- b)  I2C1-SR1 h@ 1 bit and 0<> ;
: i2c-stopf? ( -- b) I2C1-SR1 h@ 4 bit and 0<> ;

: i2c? cr I2C1-CR1 h@ hex. I2C1-CR2 h@ hex. I2C1-SR1 h@ hex. I2C1-SR2 h@ hex. ;
: i2c-start ( -- )  8 bit I2C1-CR1 hbis! begin i2c-sb?    until ;
: i2c-stop  ( -- )  9 bit I2C1-CR1 hbis! ; \ begin i2c-stopf? until ;
: i2c-addr? ( -- b ) I2C1-SR1 h@ 1 bit and 0<> ;
: i2c-w-addr ( addr -- ) i2c-sb? if  shl      I2C1-DR c! else ." ?" then begin i2c-addr? until i2c-master? drop ;
: i2c-r-addr ( addr -- ) i2c-sb? if  shl 1 or I2C1-DR c! else ." ?" then begin i2c-addr? until i2c-master? drop ;
: i2c-nak? ( -- b) I2C1-SR1 h@ 10 bit and 0<> ;
: i2c-probe ( addr -- nak) i2c-sb? if  shl      I2C1-DR c! else ." ?" then  i2c-addr? i2c-master? 2drop i2c-nak?  ; 

: i2c-txe? ( -- b) I2C1-SR1 h@ 7  bit and 0<> ;

\ TODO: read SR2 somewhere
\ TODO: wait loop is vulnerable to nonexistant adresses

\ Testing
\ TODO: error handling
: iiic cr
       i2c-reset
       begin i2c-busy? 0= until
       i2c? ." after reset"
       \ start
       i2c-start
       i2c? ." after start"
       \ begin i2c-master? until
       \ EV5: address sending
       $39 i2c-w-addr   \ checks for SB set
       i2c? ." after write address"
;

: iiid cr
       i2c-reset
       
       \ i2c? ." after reset"
       \ start
       i2c-start
       \ i2c? ." after start"
       \ begin i2c-master? until
       \ EV5: address sending
       i2c-w-addr   \ checks for SB set
       \ i2c? ." after write address"
       \ cr
       i2c-nak? if ." No response from device" else ." Device responded" then
       i2c-stop
       \ i2c? ." after stop"
;

: i2c. ( -- )  \ scan and report all I2C devices on the bus
  128 0 do
    cr i h.2 ." :"
    16 0 do  space
      i j +  dup i2c-start i2c-probe if drop ." --" else h.2 then i2c-stop
    loop
  16 +loop ;


\ iiic 

\ non existent device:
\ 00000401 00000002 00000000 00000000 after reset
\ 00000401 00000002 00000001 00000003 after start
\ 00000401 00000002 00000400 00000003 after write address

\ existing device:
\ 00000401 00000002 00000000 00000000 after reset
\ 00000401 00000002 00000001 00000003 after start
\ 00000401 00000002 00000082 00000007 after write address ok.

\ start
\ i2c-start

\ EV5
\ SB=1, cleared by reading SR1 register followed by writing DR register with Address.
\ Address
\ $39 i2c-addr

\ ACK

\ EV6
\ ADDR=1, cleared by reading SR1 register followed by reading SR2.

\ EV8_1
\ TxE=1, shift register empty, data register empty, write Data1 in DR.

\ Data1
\ EV8
\ TxE=1, shift register not empty,.data register empty, cleared by writing DR register

\ ACK

\ Data2
\ EV8

\ ACK

\ etc

\ EV8_2
\ TxE=1, BTF = 1, Program Stop request. TxE and BTF are cleared by hardware by the Stop condition

\ Stop

