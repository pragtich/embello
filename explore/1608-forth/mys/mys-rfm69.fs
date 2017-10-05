
( Mysensors driver for rfm69 radio

\ Protocol version 2.0, new style rfm69

\ In development, does not support everything

\ Strongly based on https://github.com/jeelabs/embello/blob/master/explore/1608-forth/flib/spi/rf69.fs

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

include ../flib/any/ring.fs

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
  3C05 h, \ fifo thres TODO RFM69_FIFOTHRESH_TXSTART_FIFOTHRESH | (RFM69_HEADER_LEN - 1)  = 0 | (6-1)=5
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

create mys:findparent
hex 
11 c,
11 c,
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
  0 do dup c@  dup h.2  RF:FIFO rf! 1+ loop drop ;

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

: rf. ( -- )  \ print out all the RF69 registers
  cr 4 spaces  base @ hex  16 0 do space i . loop  base !
  $60 $00 do
    cr
    i h.2 ." :"
    16 0 do  space
      i j + ?dup if rf@ h.2 else ." --" then
    loop
    $10 +loop ;


( Receive ring )
16 mys-MAXLEN * 4 + buffer: buf  buf 16 init-ring


." Testing RFM69 Mysensors "
rf-init  

." Sending test message "

mys:findparent 7 0 rf-send
( mys:testmsg 9 0 rf-send   )
