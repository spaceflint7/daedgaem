
; calculate size of the (emulated) processor prefetch queue

    bits 16
    org 100h

    lea  bx, target
    mov  al, 40h
    jmp  loop
loop:
    mov  [bx], al
target:
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    nop
    cmp al, 40h
    jne done
    mov byte [bx], 90h
    inc bx
    jmp loop
done:
    lea dx, loop
    sub bx, dx
    int 20h

STRING db "PREFETCH LENGTH IN HEX = "
LENGTH db "XX", 10, 13, "$"
TABLE db "0123456789ABCDEF"
