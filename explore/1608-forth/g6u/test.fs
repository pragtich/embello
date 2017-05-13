\ board definitions

\ eraseflash
( compiletoflash )
( board start: ) here dup hex.

hello

include ./i2c.fs

include ./ssd1306.fs


hello

i2c-init i2c.

: t lcd-init show-logo ;

