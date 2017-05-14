\ board definitions

\ eraseflash
( compiletoflash )
( board start: ) here dup hex.

hello

0 constant OLED.LARGE

include ./i2c.fs

include ./ssd1306.fs


hello

i2c-init i2c.

: t lcd-init show-logo ;

