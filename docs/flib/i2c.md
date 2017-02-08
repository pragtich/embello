# I2C communication driver

* Code: `any/i2c-bb.fs`

This describes the portable _bit-banged_ version of the I2C driver.

Each I2C transaction consists of the following steps:

* start the transaction by calling `i2c-addr`
* send all the bytes out with repeated calls to `>i2c` (or none at all)
* give the number of expected bytes read back to `i2c-xfer` (can be 0)
* check the result to verify that the device responded (false means ok)
* read the reply bytes with repeated calls to `i2c>` (or none at all)
* the transaction will be closed by the driver when the count is reaced

### API

```
: i2c-init ( -- )  \ initialise bit-banged I2C
: i2c-addr ( u -- )  \ start a new I2C transaction
: i2c-xfer ( u -- nak )  \ prepare for the reply
: >i2c ( u -- )  \ send one byte out to the I2C bus
: i2c> ( -- u )  \ read one byte back from the I2C bus
: i2c. ( -- )  \ scan and report all I2C devices on the bus

```

### Examples

T.B.D.