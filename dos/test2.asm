    bits 16
    org 100h

    ;mov ax, 0001h
    ;int 10h

    mov ax, 0b800h
    mov es, ax
    mov di, 8
    mov si, 1F40h
L1:
    mov word es:[di], si
    inc si
    add di, 40 * 2
    cmp di, 40 * 2 * 20
    jbe L1

    int 20h
