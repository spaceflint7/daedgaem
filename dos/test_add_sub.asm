    bits 16
    org 100h

;
; TEST ADD AND SUB
;

    mov si, 0
L1: mov di, 0

L2: mov bx, si
    clc
            ;add bx, di
             sub bx, di
    pushf

    mov dx, bx
    lea bp, STRING
    call N2H

    pop dx
    and dx, 07FFFh
    add bp, 6
    call N2H

    mov dx, si
    add bp, 8
    call N2H

    mov dx, di
    add bp, 7
    call N2H

    mov ah, 9
    lea dx, STRING
    int 21h

    mov ax, si
    add al, ah
    xor ah, ah
    add ax, 3
    add di, ax

    jno L2
    inc si
    jnz L1

    int 20h

;STRING db "XXXX (XXXX) = XXXX + XXXX"
STRING db "XXXX (XXXX) = XXXX - XXXX"
    db 13, 10, "$"

    ; DX = number to print
    ; BP = string to write
    ; destroys AX, BX
N2H:
    lea bx, TABLE
    mov al, dl
    and al, 0fh
    xlat
    mov [bp+3], al
    mov al, dl
    shr al, 1
    shr al, 1
    shr al, 1
    shr al, 1
    xlat
    mov [bp+2], al
    mov al, dh
    and al, 0fh
    xlat
    mov [bp+1], al
    mov al, dh
    shr al, 1
    shr al, 1
    shr al, 1
    shr al, 1
    xlat
    mov [bp+0], al
    ret

TABLE db "0123456789ABCDEF"
