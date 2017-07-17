

\ Testing package
\ Assumes Oled at $3C, PCF8574 at $3f and ADS1113 at $48

include ttester.fs

." Starting tests: " cr cr ." ------------- " cr
T{ $3c i2c-probe -> 0  }T
T{ $3d i2c-probe -> -1 }T
T{ $48 i2c-addr 3 >i2c $ab >i2c $cd >i2c 0 i2c-xfer -> 0 }T
T{ $48 i2c-addr 3 >i2c $ab >i2c  0 i2c-xfer -> 0 }T
$48 i2c-addr 3 >i2c 0 i2c-xfer
T{ $48 i2c-addr 1 i2c-xfer i2c> -> 0 $ab }T
T{ $48 i2c-addr 2 i2c-xfer i2c>h_inv -> 0 $abcd }T
T{ $48 i2c-addr 3 >i2c $fe >i2c $dc >i2c 2 i2c-xfer i2c>h_inv -> 0 $fedc }T

: send10 10 0 do i >i2c loop ;
T{ $3f i2c-addr  send10 0 i2c-xfer -> 0 }T

." Tests done"
