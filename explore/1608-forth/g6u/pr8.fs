
\ test intro
forgetram
compiletoram

\ blink definitions
PC13 constant led 
: led-init omode-pp led io-mode! ;
: led-on led ioc! ;
: led-off led ios! ;
: led-toggle led iox! ;

: blink led-init begin led-toggle 500 ms key? until led-off ;


\ test
\ blink

\ i2c is already loaded somewhere



