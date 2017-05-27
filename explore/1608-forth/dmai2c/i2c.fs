\ Hardware I2C driver for STM32F103.

\ Define pins
[ifndef] SCL  PB6 constant SCL  [then]
[ifndef] SDA  PB7 constant SDA  [then]

\ Buffers
[ifndef] i2c-bufsize 16 constant i2c-bufsize [then]

i2c-bufsize 4 + buffer: i2c.txbuf
i2c-bufsize 4 + buffer: i2c.rxbuf
i2c.txbuf i2c-bufsize init-ring
i2c.rxbuf i2c-bufsize init-ring

\ Register definitions
$40005400 constant I2C1
$40005800 constant I2C2
     I2C1 $00 + constant I2C1-CR1
     I2C1 $04 + constant I2C1-CR2
     I2C1 $10 + constant I2C1-DR
     I2C1 $14 + constant I2C1-SR1
     I2C1 $18 + constant I2C1-SR2
     I2C1 $1C + constant I2C1-CCR
     I2C1 $20 + constant I2C1-TRISE
\ $40021000 constant RCC
     RCC $10 + constant RCC-APB1RSTR
     RCC $14 + constant RCC-AHBENR

\ Register constants
3  bit constant APB2-GPIOB-EN
0  bit constant APB2-AFIO-EN
21 bit constant APB1-I2C1-EN
21 bit constant APB1-RST-I2C1

\ Checks I2C1 busy bit
: i2c-busy?   ( -- b) I2C1-SR2 h@ 1 bit and 0<> ;

\ Init and reset I2C. Probably overkill. TODO simplify
: i2c-init ( -- )
  \ Reset I2C1
  APB1-RST-I2C1 RCC-APB1RSTR bis!
  APB1-RST-I2C1 RCC-APB1RSTR bic!

  \ init clocks
  APB2-GPIOB-EN APB2-AFIO-EN or RCC-APB2ENR bis!
  APB1-I2C1-EN                  RCC-APB1ENR bis!

  \ init GPIO
  IMODE-FLOAT SCL io-mode!  \ edited: manual says use floating input
  IMODE-FLOAT SDA io-mode!
  OMODE-AF-OD OMODE-FAST + SCL io-mode!  \ I²C requires external pullup
  OMODE-AF-OD OMODE-FAST + SDA io-mode!  \     resistors on SCL and SDA

  \ Reset I2C peripheral
   15 bit I2C1-CR1 hbis!
   15 bit I2C1-CR1 hbic!

  \ Enable I2C peripheral
  21 bit RCC-APB1ENR bis!  \ set I2C1EN
  $3F I2C1-CR2 hbic!       \ CLEAR FREQ field
  36 I2C1-CR2 hbis!        \ APB1 is 36 MHz TODO: all clock rates

  \ Configure clock control registers. For now, 100 kHz and 1000us rise time
  \ TODO: configure variable bit rates, Fast mode
  $B4 I2C1-CCR h!          \ Select 100 kHz normal mode
  37  I2C1-TRISE h!        \ APB1(MHz)+1 for 1000ns SCL

  0  bit 10 bit or I2C1-CR1 hbis!    \ ACK enable, Enable bit

  begin i2c-busy? 0= until \ Wait until peripheral ready
 ;

\ debugging
: i2c? cr I2C1-CR1 h@ hex. I2C1-CR2 h@ hex. I2C1-SR1 h@ hex. I2C1-SR2 h@ hex. ;


\ API (should be compatible with i2c-bb

: i2c-addr ( u --) \ Start a new transaction and send address in write mode
;

: i2c-xfer ( u -- nak ) \ prepares for reading an nbyte reply.
                        \ Use after i2c-addr. Stops i2c after completion.
;

: >i2c  ( u -- ) \ Sends a byte over i2c. Use after i2c-addr
;

: i2c>
;

: i2c>h
    i2c>   i2c>  8 lshift or
;

: i2c>h_inv
    i2c>  8 lshift i2c>  or
;

\ Own additions to API

: i2c-probe ( addr -- nak )
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
