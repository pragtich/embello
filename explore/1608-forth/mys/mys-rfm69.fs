
( Mysensors driver for rfm69 radio

Protocol version 2.0, new style rfm69

In development, does not support everything

Tesing agains g6s.fs default

Pinout: https://jeelabs.org/article/1649e/
RFM69CW 	STM32F103 	BOTH
SSEL (NSS) 	PA4 	+3.3V
SCLK 	        PA5 	GND
MISO     	PA6 	
MOSI     	PA7
)

\ needs ring.fs
\ needs multi.fs
\ needs rfm69.fs ?

include ring.fs
include rfm69.fs

PA11 constant mys-pin-DIO0
PA12 constant mys-pin-DIO1
PA15 constant mys-pin-DIO2
PB3  constant mys-pin-DIO3
PB4  constant mys-pin-DIO4
PB5  constant mys-pin-DIO5


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
