
; print value in port 03DA as it changes over time

    bits 16
    org 100h

    xor ax,ax
    mov es,ax
    mov cx, 9999h

L0:
    mov dx,3DAh
    in al, dx
    mov bl, al
L1:
    mov dx,3DAh
    in al, dx
    cmp al, bl
    je  L1
    xor ah, ah
    push ax

    mov dx, ax
    mov bp, STRING+0
    call N2H

    mov dx, es:[046EH]
    add bp, 7
    call N2H
    mov dx, es:[046CH]
    add bp, 5
    call N2H

    mov ah, 9
    lea dx, STRING
    int 21h

    pop bx
    loop L1

    int 20h

STRING db "XXXX @ XXXX:XXXX"
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
