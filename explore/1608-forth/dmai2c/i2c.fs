pc13 constant led

: led-init             omode-pp led io-mode! ;
: led-on               led ioc! ;
: led-off              led ios! ;
: on-cycle   ( n -- )  led-on ms led-off ;
: off-cycle  ( n -- )  20 swap - ms ;
: cycle      ( n -- )  dup on-cycle off-cycle ;
: dim        ( n -- )  led-init begin dup cycle key? until drop ;


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

\ Variables
0 variable i2c.addr
0 variable i2c.cnt

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
\ $40020000 constant DMA1
   \ DMA1 $00 + constant DMA1-ISR
   \ DMA1 $04 + constant DMA1-IFCR

    DMA1 20 6 1- * + 
    dup $08 + constant DMA1-CCR6
    dup $0c + constant DMA1-CNDTR6
    dup $10 + constant DMA1-CPAR6
    dup $14 + constant DMA1-CMAR6
    drop
    
    DMA1 20 7 1- * + 
    dup $08 + constant DMA1-CCR7
    dup $0c + constant DMA1-CNDTR7
    dup $10 + constant DMA1-CPAR7
    dup $14 + constant DMA1-CMAR7
    drop
         
    $E000E000 constant NVIC  
    NVIC $4 + constant NVIC_ICTR
    NVIC $F00 + constant NVIC_STIR
    NVIC $100 + constant NVIC_ISER0
    NVIC $104 + constant NVIC_ISER1
    NVIC $180 + constant NVIC_ICER0
    NVIC $184 + constant NVIC_ICER1
    NVIC $200 + constant NVIC_ISPR0
    NVIC $204 + constant NVIC_ISPR1
    NVIC $280 + constant NVIC_ICPR0
    NVIC $284 + constant NVIC_ICPR1
    NVIC $300 + constant NVIC_IABR0
    NVIC $304 + constant NVIC_IABR1
    NVIC $400 + constant NVIC_IPR0
    NVIC $404 + constant NVIC_IPR1
    NVIC $408 + constant NVIC_IPR2
    NVIC $40C + constant NVIC_IPR3
    NVIC $410 + constant NVIC_IPR4
    NVIC $414 + constant NVIC_IPR5
    NVIC $418 + constant NVIC_IPR6
    NVIC $41C + constant NVIC_IPR7
    NVIC $420 + constant NVIC_IPR8
    NVIC $424 + constant NVIC_IPR9
    NVIC $428 + constant NVIC_IPR10
    NVIC $42C + constant NVIC_IPR11
    NVIC $430 + constant NVIC_IPR12
    NVIC $434 + constant NVIC_IPR13
    NVIC $438 + constant NVIC_IPR14    

\ Register constants
3  bit constant APB2-GPIOB-EN
0  bit constant APB2-AFIO-EN
21 bit constant APB1-I2C1-EN
21 bit constant APB1-RST-I2C1

0  bit constant i2c-SR1-SB
10 bit constant i2c-SR1-AF
1  bit constant i2c-SR2-BUSY

\ status checks
: i2c-SR1-flag? I2C1-SR1 hbit@ ;
: i2c-SR2-flag? I2C1-SR2 hbit@ ;
: i2c-SR1-wait ( u -- ) i2c.timeout @ begin 1- 2dup 0= swap i2c-SR1-flag? or until 2drop ; \ Waits until SR1 meets bit mask or timeout
: i2c-busy?   ( -- b) i2c-SR2-BUSY i2c-SR2-flag? 0<> ;

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

  0  bit RCC-AHBENR bis!   \ Enable DMA peripheral clock
  0  bit DMA1-CCR6  bic!   \ Disable it for now (ch 6 = I2C1 TX)
 ;

\ debugging
: i2c? cr I2C1-CR1 h@ hex. I2C1-CR2 h@ hex. I2C1-SR1 h@ hex. I2C1-SR2 h@ hex. ;

\ Low level register setting and reading
: i2c-start! ( -- ) 8 bit I2C1-CR1 hbis! ;
: i2c-start \ set start bit and wait for start condition
  i2c-start! i2c-SR1-SB i2c-SR1-wait ;
: i2c-stop!   ( -- )    9 bit I2C1-CR1 hbis! ;
: i2c-stop  ( -- )  i2c-stop! i2c-SR2-MSL i2c-SR2-!wait ; \ stop and wait

: i2c-DR!     ( c -- )  I2C1-DR c! ;            \ Writes data register

\ STM Events
: i2c-EV8_2 i2c-SR1-BTF  i2c-SR1-wait ;

\ API (should be compatible with i2c-bb

: i2c-addr ( u --)
  \ Start a new transaction and send address in write mode
  \ Does not wait for ADDR: i2x-xfer should do this
  i2c.txbuf i2c-bufsize init-ring
  i2c.rxbuf i2c-bufsize init-ring

  i2c-start
  shl dup i2c.addr !
  \ i2c-EV5
  i2c-SR1-SB i2c-SR1-wait
  i2c-DR!                   \ Sends address (write mode)
;

: i2c-irq-tx-stop           \ irq handler for DMA transmission done
  led-off
  0 bit DMA1-CCR6 bic!
  11 bit I2C1-CR2 hbic!
  21 bit DMA1-IFCR bis!     \ CTCIF6, clear transfer complete flag
  i2c-EV8_2 i2c-stop
  begin 9 bit I2C1-CR1 hbit@ 0= until \ Wait for STOP to clear
;

: i2c-irq-rx-stop
  led-off
  11 bit I2C1-CR2  hbic!     \ DMAEN
  0  bit DMA1-CCR7  bic!     \ DMAEN
  25 bit DMA1-IFCR  bis!     \ CTCIF7
  i2c-stop
  begin 9 bit I2C1-CR1 hbit@ 0= until \ Wait for STOP to clear

  \ Let X buffer know of new data
  i2c.cnt @ i2c.rxbuf 1+ !
;

\ TODO
\
\ $3f i2c-addr $ff >i2c 1 i2c-xfer  ok.
\ i2c.cnt @ . 536872105  ok.

: i2c-irq-tx-rx             \ irq handler for DMA transmission done
  led-on
  0 bit DMA1-CCR6 bic!
  11 bit I2C1-CR2 hbic!
  21 bit DMA1-IFCR bis!     \ CTCIF6, clear transfer complete flag

  \ Configure DMA
  %0011000010001010 DMA1-CCR7 !              \ 8 bit high prio periph to mem error&finish interrupt
  ['] i2c-irq-rx-stop irq-dma1_7 !
  17 bit        NVIC_ISER0  !
  i2c.rxbuf 4 + DMA1-CMAR7  !
  I2C1-DR       DMA1-CPAR7  !
  i2c.cnt @     DMA1-CNDTR7 !              \ Count
  \ Start transmission (will wait for I2C1 to be ready)
  11 bit I2C1-CR2  hbis!                     \ DMAEN
  12 bit I2C1-CR2  hbis!                     \ LAST
  0  bit DMA1-CCR7 bis!                      \ DMAEN
  \ Send start  
  i2c-start                 \ Restart
  i2c.addr @ 1 or i2c-DR!   \ Read address
  i2c-EV6
  led-on
;

: i2c-xfer ( u -- nak ) \ prepares for reading an nbyte reply.
  \ Use after i2c-addr and optional >i2c calls.

  dup i2c.cnt !
  \ Register end of transmission handler
  dup 0= if
    ['] i2c-irq-tx-stop irq-dma1_6 !
  else
    ['] i2c-irq-tx-rx   irq-dma1_6 !
  then
  \ Wait for addr and clear it
  i2c-SR1-ADDR i2c-SR1-AF or i2c-SR1-wait    \ Wait for ack or nak
  I2C1-SR2 h@ drop                           \ clear ACK flag
  i2c-SR1-AF i2c-SR1-flag?                   \ Put NAK on stack
  i2c-SR1-AF i2c1-SR1 hbic!                  \ Clear the NAK flag

  \ Stop if nothing to send

  i2c.txbuf ring# 0= if
    i2c-stop
    begin 9 bit I2C1-CR1 hbit@ 0= until
  else
    \ Configure DMA
    %0011000010011010 DMA1-CCR6 !              \ 8 bit high prio mem to peripheral error&finish interrupt
    16 bit NVIC_ISER0 !
    i2c.txbuf 4 + DMA1-CMAR6 !
    I2C1-DR       DMA1-CPAR6 !
    i2c.txbuf ring# DMA1-CNDTR6 !              \ Count
    \ Start transmission (will wait for I2C1 to be ready)
    11 bit I2C1-CR2 hbis!                      \ DMAEN
    0 bit DMA1-CCR6 bis!                       \ DMAEN

  then
;

: >i2c  ( u -- ) \ Sends a byte over i2c. Use after i2c-addr
  i2c.txbuf >ring ;

: i2c>
  begin i2c.rxbuf ring# 0<> until 
  i2c.rxbuf ring> ;

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



\ Debugging

: blip $3f i2c-addr $ff >i2c $0 >i2c 0 i2c-xfer . i2c? cr ;
: blip2 $3f i2c-addr 15 0 do i dup . >i2c loop 0 i2c-xfer . i2c? cr ;

led-init led-off
i2c? cr
i2c-init
$40 i2c-addr 0 i2c-xfer .
i2c? cr
$41 i2c-addr 0 i2c-xfer .

