( Mysensors driver for rfm69 radio

\ Protocol version 2.0, new style rfm69

\ In development, does not support everything

\ Strongly based on https://github.com/jeelabs/embello/blob/master/explore/1608-forth/flib/spi/rf69.fs

\ Debugging with slow SPI: $0074 SPI1-CR1 !

\ Tesing agains g6s.fs default

\ Pinout: https://jeelabs.org/article/1649e/
\ RFM69CW 	STM32F103 	BOTH
\ SSEL (NSS) 	PA4 	+3.3V
\ SCLK 	        PA5 	GND
\ MISO     	PA6 	
\ MOSI     	PA7
\ )

\ needs ring.fs
\ needs multi.fs
\ needs rfm69.fs ?

compiletoram
\ include ../flib/mecrisp/hexdump.fs		
\ include ../flib/stm32f1/io.fs
\ include ../flib/stm32f1/hal.fs
\ include ../flib/pkg/pins48.fs
\ include ../flib/any/ring.fs
\ include ../flib/mecrisp/multi-irq.fs
include fsm.fs

PA11 constant mys-pin-DIO0
PA12 constant mys-pin-DIO1
PA15 constant mys-pin-DIO2
PB3  constant mys-pin-DIO3
PB4  constant mys-pin-DIO4
PB5  constant mys-pin-DIO5

       $00 constant RF:FIFO
       $01 constant RF:OP
       $07 constant RF:FRF
       $11 constant RF:PA
       $18 constant RF:LNA
       $1F constant RF:AFC
       $24 constant RF:RSSI
       $27 constant RF:IRQ1
       $28 constant RF:IRQ2
       $2F constant RF:SYN1
       $31 constant RF:SYN3
       $39 constant RF:ADDR
       $3A constant RF:BCAST
       $3C constant RF:THRESH
       $3D constant RF:PCONF2
       $3E constant RF:AES

0 2 lshift constant RF:M_SLEEP
1 2 lshift constant RF:M_STDBY
2 2 lshift constant RF:M_FS
3 2 lshift constant RF:M_TX
4 2 lshift constant RF:M_RX

       $C2 constant RF:START_TX
       $42 constant RF:STOP_TX
       $80 constant RF:RCCALSTART

     7 bit constant RF:IRQ1_MRDY
     6 bit constant RF:IRQ1_RXRDY
     3 bit constant RF:IRQ1_RSSI
     2 bit constant RF:IRQ1_TIMEOUT
     0 bit constant RF:IRQ1_SYNC

     6 bit constant RF:IRQ2_FIFO_NE
     3 bit constant RF:IRQ2_SENT
     2 bit constant RF:IRQ2_RECVD
     1 bit constant RF:IRQ2_CRCOK

     0 variable rf.mode \ last set chip mode

64  constant mys:MAXLEN \ TODO: how long?
5   constant mys:nHDR   \ Header length
2   constant mys:VER    \ mysensors version: TODO
$FF variable mys:myparent

create mys:rxmsg mys:MAXLEN allot 

create mys-rf:init  \ initialise the radio, each 16-bit word is <reg#,val>
\ TODO: how much can I reuse from the rfm69 driver?
hex
  0200 h, \ packet mode, fsk
  0302 h, 0440 h, \ bit rate TODO
  0505 h, 06C3 h, \ 90.3kHzFdev -> modulation index = 2
  0B20 h, \ low M TODO Mysensors does not set this
  1888 h, \ TODO: mysensors does this; RFM69_REG_LNA, RFM69_LNA_ZIN_200 | RFM69_LNA_CURRENTGAIN
  1942 h, 1A42 h, \ RxBw 125khz, AFCBw 125khz same in mysensors Todo mysensors does not set AFCBW
  1E0C h, \ AFC auto-clear, auto-on TODO mysensors does not do this
  2607 h, \ disable clkout
  2810 h, \ TODO  RFM69_REG_IRQFLAGS2, RFM69_IRQFLAGS2_FIFOOVERRUN 
  29dc h, \ RSSI thres TODO mysensors=220
  2B40 h, \ RSSI timeout after 128 bytes TODO mysensors does not do this
  2C00 h, \ Preamble MSB=0
  2D03 h, \ Preamble 5 bytes TODO mysensors = 3
  2E88 h, \ TODO sync size 2 bytes
  2F2D h, \ TODO removed 1st AA byte
  3064 h, \ TODO make varible: network group default 100
  37D4 h, \ Todo RFM69_PACKET1_FORMAT_VARIABLE | RFM69_PACKET1_DCFREE_WHITENING | RFM69_PACKET1_CRC_ON | RFM69_PACKET1_CRCAUTOCLEAR_ON | RFM69_PACKET1_ADRSFILTERING_NODEBROADCAST 
  3842 h, \ TODO max 66 byte payload
  39ff h, \ TODO mysensors has this, RFM69_BROADCAST_ADDRESS=255 = startup value
  3aff h, \ TODO mysensors has this, RFM69_BROADCAST_ADDRESS=255
  \ 3C05 h, \ fifo thres TODO RFM69_FIFOTHRESH_TXSTART_FIFOTHRESH | (RFM69_HEADER_LEN - 1)  = 0 | (6-1)=5
 \ 3C8F h,
  3C07 h,
  3D10 h, \ TODO RFM69_PACKET2_RXRESTARTDELAY_2BITS | RFM69_PACKET2_AUTORXRESTART_OFF | RFM69_PACKET2_AES_OFF
  6F30 h, \ TODO Te st DAGC lowbeta 0
  0 h,  \ sentinel
decimal align

create mys:testmsg
hex
11 c,
11 c,
00 c,
12 c,
41 c,
10 c,
03 c,
00 c,
00 c,
decimal calign

create mys:oldparent
hex 
11 c,
11 c,
ff c,
02 c,
03 c,
07 c,
ff c,
decimal calign

create mys:findparent
hex
07 c,
ff c,
ff c,
ff c,
02 c,
03 c,
07 c,
ff c,
decimal calign

\ r/w access to the RF registers
: rf!@ ( b reg -- b ) +spi >spi >spi> -spi ;
: rf! ( b reg -- ) $80 or rf!@ drop ;
: rf@ ( reg -- b ) 0 swap rf!@ ;

: rf-h! ( h -- ) dup $FF and swap 8 rshift rf! ;

: rf-n@spi ( addr len -- )  \ read N bytes from the FIFO
  0 do  RF:FIFO rf@ over c! 1+  loop drop ;
: rf-n!spi ( addr len -- )  \ write N bytes to the FIFO
  ( ." >" ) 0 do dup c@  ( dup h.2 ) RF:FIFO rf! 1+ loop drop ( ." < " ) ;

: rf!mode ( b -- )  \ set the radio mode, and store a copy in a variable
  dup rf.mode !
  RF:OP rf@  $E3 and  or RF:OP rf!
  begin  RF:IRQ1 rf@  RF:IRQ1_MRDY and  until ;


: rf-config! ( addr -- ) \ load many registers from <reg,value> array, zero-terminated
  RF:M_STDBY rf!mode \ some regs don't program in sleep mode, go figure...
  begin  dup h@  ?dup while  rf-h!  2+ repeat drop
;

: rf-freq ( u -- )  \ set the frequency, supports any input precision
  begin dup 100000000 < while 10 * repeat
  ( f ) 2 lshift  32000000 11 rshift u/mod nip  \ avoid / use u/ instead
  ( u ) dup 10 rshift  RF:FRF rf!
  ( u ) dup 2 rshift  RF:FRF 1+ rf!
  ( u ) 6 lshift RF:FRF 2+ rf!
;

: rf-check ( b -- )  \ check that the register can be accessed over SPI
  begin  dup RF:SYN1 rf!  RF:SYN1 rf@  over = until
  drop ;

: rf-ini (  freq -- )  \ internal init of the RFM69 radio module
  spi-init
  $AA rf-check  $55 rf-check  \ will hang if there is no radio!
  mys-rf:init rf-config!
  rf-freq ; \ rf-group ;

: rf-init ( -- )  \ init RFM69 with current rf.group and rf.freq values
  868000000 rf-ini ;

: rf. ( -- )  \ print out all the RF69 registers
  cr 4 spaces  base @ hex  16 0 do space i . loop  base !
  $60 $00 do
    cr
    i h.2 ." :"
    16 0 do  space
      i j + ?dup if rf@ h.2 else ." --" then
    loop
    $10 +loop ;

: rf-send ( addr count recip -- )  \ send out one packet to recipient recip
  RF:M_STDBY rf!mode
  \ n
  \ recip
  \ ver
  \ sender
  \ cflags
  \ seq
  over 5 + RF:FIFO rf!                     \ n+6
           RF:FIFO rf!                     \ recip
  2        RF:FIFO rf!                     \ version
  123      RF:FIFO rf!                     \ sender (TODO)
  0        RF:FIFO rf!                     \ flags (TODO)
  1        RF:FIFO rf!                     \ seq (TODO)
  ( addr count )  rf-n!spi                  \ body
  RF:M_TX rf!mode
  begin RF:IRQ2 rf@ RF:IRQ2_SENT and until
  RF:M_STDBY rf!mode ;


: mys-send ( caddr -- )  \ send out one preformatted packet
  RF:M_STDBY rf!mode
  \ RFM69 header:
  \ n (complete packet excluding count byte)
  \ recipient
  \ version
  \ sender
  \ flags
  \ sequence
  \ Packet format:
  \ 0 last
  \ 1 sender
  \ 2 destination
  \ 3 version_length (2 bit protocol ver, 1 bit signed flag, 5 bit length of payload
  \ 4 command_ack_payload (3 bit command type, 1 bit request ACK, 1 bit IS ACK, 3 bit payload data type)
  \ 5 type (depends on command)
  \ 6 sensor (sensor ID)
  \ 7+ payload or ack
  \ c++@ swap dup 0 do ( caddr+1 c0 )
  \   dup h.2 ." ." RF:FIFO rf! c++@ swap ( caddr+1 c1 )
  \ loop
  \ 2drop

  dup c@ mys:nHDR + RF:FIFO rf!
  dup 1+  c@        RF:FIFO rf!  \ recipient
  dup 2 + c@        RF:FIFO rf!  \ version
  dup 3 + c@        RF:FIFO rf!  \ sender
  dup 4 + c@        RF:FIFO rf!  \ flags
  dup 5 + c@        RF:FIFO rf!  \ sequence
  
  c++@ swap \ dup RF:FIFO rf!
  
  rf-n!spi
  \  rf.
  RF:M_TX rf!mode
  begin RF:IRQ2 rf@ RF:IRQ2_SENT and until
  RF:M_STDBY rf!mode ;    
 

( Receive ring , Transmit ring )
256 4 + buffer: rxbuf  rxbuf  256 init-ring
256 4 + buffer: txbuf  txbuf  256 init-ring
( Ring for transport state machine updates)
256 4 + buffer: tsmbuf tsmbuf 256 init-ring


: >mys  ( caddr -- ) \ Adds message (array of chars) starting at addr to txbuf
  txbuf ring? if \ TODO: proper length checking
    dup c@ 1+ 0 do dup c@ ( dup h.2 space ) txbuf >ring 1+ loop
  else
    ." Error: tx buf full, dropping message"
  then
  drop
;

: c!++ ( c c-addr -- c-addr + 1)
  dup -rot c! 1+ ;

: ring>cstr ( ring c-addr)
  over ring>              ( ring c-addr # )
  swap over               ( ring # c-addr # )
  0 do                    ( ring # c-addr )
    c!++ over ring> swap  ( ring c1+ c-addr+1 )
  loop
  c! drop ;


: mys-available? ( -- bool) \ Check if there is an incoming message in the FIFO
  rf.mode @  RF:M_STDBY = if
    RF:M_RX rf!mode
    
    0 
  else
  
    RF:IRQ2 rf@ RF:IRQ2_CRCOK and 0<>
  then ;

: mys-dump ( caddr -- ) \ Dump packet contents
  dup hex.
  mys:MAXLEN dump
  ;

: mys-msg>parent ( caddr -- parent ) \ Get parent response from packet (no RFM69 header) or $FF if invalid
  dup      c@ 13 =                ( caddr flag ) \ TODO: how long?
  over 10 + c@ $23 = and          \ command=1, c=3
  over 11 + c@ $8  = and          \ t=8
  if
  \ Get address and return it
    13 + c@
  else
    drop $FF
  then
;

: mys-waitparent ( ms -- parent ) \ Wait for a parent confirmation in ms milliseconds or returns $FF
  ( dup . ." milliseconds to wait")
  0 do
    
    mys-available? if
      RF:FIFO rf@ mys:MAXLEN min           ( n )
      dup mys:rxmsg c!                     ( n )
      mys:rxmsg 1+ over                    ( n rxmsg+1 n )   
      rf-n@spi                             ( n )
      mys:rxmsg mys-msg>parent
      dup $FF <> if unloop exit then
    then
    1 ms
  loop
  RF:M_STDBY rf!mode
  $FF ;

0 constant mys:INIT    \ 0 initialize
1 constant mys:PARENT  \ 1 find parent
2 constant mys:ERROR   \ 2 get ID
3 constant mys:READY   \ 3 ready
\ 4 check uplink
\ 5 fail

: mys-init ." mys-init:" cr rf-init 500 ms ." Testing RFM69 mysensors" cr ;
: mys-parent   \ TODO: multiple parent responses. Also, paybload byte is distance, not parent https://github.com/mysensors/MySensors/blob/b148d828ad149796cf700c9a6a28390736652b43/core/MyTransport.cpp#L745 and I should reply with 'sender' instead
  ." Finding parent" cr
  mys:findparent >mys
  begin pause txbuf ring# 0= until
  2000 mys-waitparent
  dup ." Parent result: " h.2 cr
  dup $FF = if
    ." Error finding parent, defaulting to 0"   \ TODO: do something smarter?
    drop 0
  then
  mys:myparent !
  0 tsmbuf >ring           \ Transport can continue
;

: mys-run                  \ Run the normal sensor task
  ." Running"
  begin
    pause
  again
  ;

: mys-error ." ERROR " cr ;

2 wide FSM: TSM
                         (  OK case )              ( NOK case )
( state=0 mys:INIT)   || mys-init   mys:PARENT || mys-error mys:ERROR
( state=1 mys:PARENT) || mys-parent mys:READY  || mys-error mys:PARENT
( state=2 mys:ERROR)  || nop        mys:INIT   || mys-error mys:ERROR
( state=3 mys:READY)  || mys-run    mys:READY  || mys-error mys:INIT
;FSM

task: transport
: transport& ( -- )
  0 ['] TSM STATE!
  transport activate
  0 TSM 0 TSM
  begin
    tsmbuf ring# if
      tsmbuf ring> TSM
    then
    pause
    5000 ms ." ."
  again ;

create mys:txmsg mys:MAXLEN allot 
task: mys-tx
: mys-tx& ( -- )
  mys-tx activate
  begin
    txbuf ring#
    if
      txbuf mys:txmsg ring>cstr
      mys:txmsg mys-send
      ( TODO make message ring )
    then
    pause
  again ;


\ multitask transport& mys-tx&
