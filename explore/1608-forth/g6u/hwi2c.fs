\ hardware i2c driver - not working yet
\ Started with https://github.com/jeelabs/embello/blob/master/explore/1608-forth/flib/stm32f1/i2c.fs

\ Aanname: wij gaan master mode doen

\ Master sender: Zie RM0008 Figure 272, Master receiver fig 273


\ Default: slave mode. Start condition
\ The peripheral input clock must be programmed in the I2C_CR2 register
\ minimal 2 MHz in Sm mode 4 MHz in Fm mode
\ Master mode is selected as soon as the Start condition is generated on the bus with a START bit.
\
\ • Program the peripheral input clock in I2C_CR2 Register in order to generate correct timings
\ • Configure the clock control registers
\ • Configure the rise time register
\ • Program the I2C_CR1 register to enable the peripheral
\ • Set the START bit in the I2C_CR1 register to generate a Start condition
\ 
\ In master mode, setting the START bit causes the interface to generate a ReStart condition at the end of the current byte transfer.
\ The SB bit is set by hardware and an interrupt is generated if the ITEVFEN bit is set
\ Then the master waits for a read of the SR1 register followed by a write in the DR register with the Slave address (see Figure 272 and Figure 273 Transfer sequencing EV5).
\ In 7-bit addressing mode, one address byte is sent.
\ As soon as the address byte is sent,
\ – The ADDR bit is set by hardware and an interrupt is generated if the ITEVFEN bit is set.
\ Then the master waits for a read of the SR1 register followed by a read of the SR2 register (see Figure 272 and Figure 273 Transfer sequencing).
\ In 7-bit addressing mode,
\ – To enter Transmitter mode, a master sends the slave address with LSB reset.
\ – To enter Receiver mode, a master sends the slave address with LSB set.
\ Following the address transmission and after clearing ADDR, the master sends bytes from the DR register to the SDA line via the internal shift register.
\ The master waits until the first data byte is written into I2C_DR (see Figure 272 Transfer sequencing EV8_1). 
\ When the acknowledge pulse is received, the TxE bit is set by hardware and an interrupt is generated if the ITEVFEN and ITBUFEN bits are set.
\ If TxE is set and a data byte was not written in the DR register before the end of the last data transmission, BTF is set and the interface waits until BTF is cleared by a read from I2C_SR1 followed bya write to I2C_DR, stretching SCL low.
\ After the last byte is written to the DR register, the STOP bit is set by software to generate a Stop condition (see Figure 272 Transfer sequencing EV8_2). The interface automatically goes back to slave mode (MSL bit cleared).
\ Stop condition should be programmed during EV8_2 event, when either TxE or BTF is set.

\ Check regelmatig BERR voor fouten
\ Check AF voor NACK
\ 


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

: i2c-init ( -- )  \ initialise I2C hardware
  IMODE-FLOAT SCL io-mode!  \ edited: manual says use floating input
  IMODE-FLOAT SDA io-mode!  
  OMODE-AF-OD PB6 io-mode!  \ %1101 AF means I2C Using external pullup
  OMODE-AF-OD PB7 io-mode!
  21 bit RCC-APB1ENR bis!  \ set I2C1EN
  1  bit I2C1-CR2 hbis!           \ 2 MHz 
  10 I2C1-CCR h!  \ lets start slow 100 kHz?
  3  I2C1-TRISE h!         \ 2+1 for 1000ns SCL
  10 bit 0 bit or I2C1-CR1 hbis!     \ PE, enable device | ACK
;

: i2c-start ( -- )  8 bit I2C1-CR1 hbis! ;
: i2c-stop  ( -- )  9 bit I2C1-CR1 hbis! ;

: >i2c ( b -- nak )  \ send one byte
  I2C1-DR h!
  begin I2C1-SR1 h@ 7 bit and until
  10 bit I2C1-SR1  2dup h@ and 0<>  -rot hbic! ;
: i2c> ( nak -- b )  \ read one byte
  10 bit I2C1-CR1  rot if bic! else bis! then
  I2C1-DR h@
  begin I2C1-SR1 h@ 6 bit and until ;

: i2c-tx ( addr -- nak )  \ start device send
  i2c-start shl >i2c ;
: i2c-rx ( addr -- nak )  \ start device receive
  i2c-start shl 1+ >i2c ;

: i2c? I2C1-CR1 h@ hex. I2C1-SR1 h@ hex. I2C1-SR2 h@ hex. ;

: i2c. ( -- )  \ scan and report all I2C devices on the bus
  128 0 do
    cr i h.2 ." :"
    16 0 do  space
      i j +  dup i2c-rx i2c-stop  if drop ." --" else h.2 then
    loop
  16 +loop ;

: i2c-test ." Starting I2C " 
i2c-init
." I2C scan "
i2c. ;

\ 39
