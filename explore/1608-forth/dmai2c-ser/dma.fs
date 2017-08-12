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
    \ DMA1 $00 + constant DMA1-ISR
    \ DMA1 $04 + constant DMA1-IFCR
    \ DMA1 $08 + constant DMA1-CCR1
    DMA1 $44 + constant DMA1-CCR4
    DMA1 $48 + constant DMA1-CNDTR4
    DMA1 $4C + constant DMA1-CPAR4
    DMA1 $50 + constant DMA1-CMAR4
    DMA1 $58 + constant DMA1-CCR5
    DMA1 $5C + constant DMA1-CNDTR5
    DMA1 $60 + constant DMA1-CPAR5
    DMA1 $64 + constant DMA1-CMAR5

    $E000E000 constant NVIC  
    NVIC $4 + constant NVIC_ICTR
    NVIC $F00 + constant NVIC_STIR
    NVIC $100 + constant NVIC_ISER0
    NVIC $104 + constant NVIC_ISER1
    NVIC $180 + constant NVIC_ICER0
    NVIC $184 + constant NVIC_ICER1
    NVIC $200 + constant NVIC_ISPR0
    NVIC $204 + constant NVIC_ISPR1
       NVIC $280 + constant NVIC_ICPR0
       NVIC $284 + constant NVIC_ICPR1
       NVIC $300 + constant NVIC_IABR0
       NVIC $304 + constant NVIC_IABR1
       NVIC $400 + constant NVIC_IPR0
       NVIC $404 + constant NVIC_IPR1
       NVIC $408 + constant NVIC_IPR2
       NVIC $40C + constant NVIC_IPR3
       NVIC $410 + constant NVIC_IPR4
       NVIC $414 + constant NVIC_IPR5
       NVIC $418 + constant NVIC_IPR6
       NVIC $41C + constant NVIC_IPR7
       NVIC $420 + constant NVIC_IPR8
       NVIC $424 + constant NVIC_IPR9
       NVIC $428 + constant NVIC_IPR10
       NVIC $42C + constant NVIC_IPR11
       NVIC $430 + constant NVIC_IPR12
       NVIC $434 + constant NVIC_IPR13
       NVIC $438 + constant NVIC_IPR14    
    
: dma-irq led-on $FFFFFFFF DMA1-IFCR !  ;

: clr dma.b 20 1 do dup i swap c! 1+  loop ;
clr
    
0  bit RCC-AHBENR bis!
0 bit dma1-ccr1 bic!
dma.a DMA1-CmAR1 !
dma.b DMA1-CpAR1 !
5  DMA1-CNDTR1 !

['] dma-irq  irq-dma1_1 !
11 bit NVIC_ISER0 !


led-init
led-off


%0111000011010010 DMA1-CCR1 !   \ Not enabled yet
dma1-cndtr1 @ .
dma.a 16 dump
dma.b 16 dump
0 bit DMA1-CCR1 bis!
100 ms
dma.a 16 dump
dma.b 16 dump
0 bit dma1-ccr1 bis!

