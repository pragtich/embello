\ A test for DMA on the STM32F103

create dma.a
$aa c,
$01 c,
$10 c,
$cc c,
$c0 c,
$ff c,
$ee c,
calign

20 buffer: dma.b

pc13 constant led

: led-init             omode-pp led io-mode! ;
: led-on               led ioc! ;
: led-off              led ios! ;
: on-cycle   ( n -- )  led-on ms led-off ;
: off-cycle  ( n -- )  20 swap - ms ;
: cycle      ( n -- )  dup on-cycle off-cycle ;
: dim        ( n -- )  led-init begin dup cycle key? until drop ;

\ $40020000 constant DMA1
\   DMA1 $00 + constant DMA1-ISR
\   DMA1 $04 + constant DMA1-IFCR
\   DMA1 $08 + constant DMA1-CCR1
    DMA1 $44 + constant DMA1-CCR4
    DMA1 $48 + constant DMA1-CNDTR4
    DMA1 $4C + constant DMA1-CPAR4
    DMA1 $50 + constant DMA1-CMAR4
    DMA1 $58 + constant DMA1-CCR5
    DMA1 $5C + constant DMA1-CNDTR5
    DMA1 $60 + constant DMA1-CPAR5
    DMA1 $64 + constant DMA1-CMAR5

: dma-irq ." !" ;
	    
    
0  bit RCC-AHBENR bis!
07 DMA1-CNDTR1 !
dma.a DMA1-CPAR1 !
dma.b DMA1-CMAR1 !
%0101000011000000 DMA1-CCR1 h!   \ Not enabled yet

['] dma-irq drop



led-init
led-off
