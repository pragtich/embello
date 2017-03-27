\ access to FAT-formatted SD cards

compiletoram? [if]  forgetram  [then]

\ include ex/sdtry.fs

0 variable sd.fat   \ block # of first FAT copy
0 variable sd.spc   \ sectors per cluster (64)
0 variable sd.root  \ block # of first root sector
0 variable sd.#ent  \ number of root entries
0 variable sd.data  \ block offset of cluster #2

: sd-c>s ( cluster -- sect ) 2- sd.spc @ * sd.data @ + ;

: sd-mount ( -- )  \ mount a FAT16 volume, extract the key disk info
                sd-init    \ initialise interface and card
              0 sd-read    \ read block #0
  sd.buf $1C6 + @          \ get location of boot sector
         dup 1+ sd.fat !   \ start sector of FAT area
            dup sd-read    \ read boot record
   sd.buf $0D + c@         \ sectors per cluster
                sd.spc !   \ depends on formatted disk size
   sd.buf $0E + h@         \ reserved sectors
   sd.buf $10 + c@         \ number of FAT copies
   sd.buf $16 + h@         \ sectors per fat
      * + + dup sd.root !  \ start sector of root directory
   sd.buf $11 + h@         \ max root entries
            dup sd.#ent !  \ save for later
     4 rshift + sd.data !  \ start sector of data area

  ." label: " sd.buf $2B + 11 type space
  ." format: " sd.buf $36 + 8 type space
  ." capacity: " sd.buf $20 + @ .
;

: dirent ( a -- a )  \ display one directory entry
  dup 2+ c@ if
    cr dup 11 type space
    dup 11 + c@ h.2 space
    dup 26 + h@ .
    dup 28 + @ .
  then ;

: ls  \ display files in root dir (skipping all LFNs)
  sd.#ent @ 16 / 0 do
    sd.root @ i + sd-read
    sd.buf  16 0 do dirent 32 + loop  drop
  loop ;

: fat-next ( u -- u )  \ return next FAT cluster, or $FFFx at end
  \ TODO hard-coded for 64 sec / 32 KB per cluster
  dup 8 rshift sd.fat @ + sd-read
  $FF and 2* sd.buf + h@ ;

: chain. ( u -- )  \ display the chain of clusters
  begin
    dup .
  dup $F or $FFFF <> while
    fat-next
  repeat drop ;

: fat-chain ( u a -- )  \ store clusters for use as file map
  begin
    2dup ! 2+
  over $F or $FFFF <> while
    swap fat-next swap
  repeat 2drop ;

: fat-map ( n a -- n )  \ map block n to raw block number, using file map
  over sd.spc @ / 2* + h@
  2- sd.spc @ * swap sd.spc @ 1- and +
  sd.data @ + ;

\ 128 clusters is 8 MB when the cluster size is 64
129 2* 4 * buffer: fat.maps  \ room for 4 file maps of max 128 clusters each

: file ( n -- a )  \ convert file 0..3 to a map address inside fat.maps
  129 2* * fat.maps + ;

: tryfat
  sd-init ." blocks: " sd-size .
  cr sd-mount ls            \ mount and show all root entries
  117 0 file fat-chain      \ build map for DISK3.IMG
  0 file 30 dump            \ show map
  0 0 file fat-map sd-read  \ load first block
  sd.buf 50 dump            \ show contents
;

\ tryfat