    bits 16
    org 100h

;
; TEST SHIFTS
;

    mov di, 0
L1: call DO_SHL
    call DO_SHR
    call DO_SAR
    call DO_ROL
    call DO_ROR
    call DO_CLC_RCL
    call DO_CLC_RCR
    call DO_STC_RCL
    call DO_STC_RCR
    inc di
    jnz L1

    int 20h

;
;
;

%macro DO_PRINT 1
    pushf

    mov dx, di
    lea bp, STRING
    call N2H

    pop dx
    ;and dx, 07FFFh
    and dx, %1
    add bp, 6
    call N2H

    mov dx, si
    add bp, 8
    call N2H

    lea bp, OPSTR + 4
    mov dx, cx
    call N2H

    mov ah, 9
    lea dx, STRING
    int 21h

%endmacro

%macro SHIFT_MACRO 1
%defstr OP_NAME %1
%substr OP_NAME_1 OP_NAME 1
%substr OP_NAME_2 OP_NAME 2
%substr OP_NAME_3 OP_NAME 3

    push di
    mov si, di
    mov byte [OPSTR],   OP_NAME_1
    mov byte [OPSTR+1], OP_NAME_2
    mov byte [OPSTR+2], OP_NAME_3

    ; do single-bit variant of operation
    %1 di, 1
    mov cx, 1
    DO_PRINT 0x78C5  ; CF, PF, ZF, SF, OF

    ; do shift-count-in-CL variant of operation
    xor cx, cx
%%LOOP_AND_PRINT_CF:
    mov di, si
    add cx, 0       ; clear AF
    clc
    %1 di, cl
    DO_PRINT 0x70C5  ; CF, PF, ZF, SF
    inc cl
    cmp cl, 16
    jne %%LOOP_AND_PRINT_CF
    ; CF is undefined when shift >= size
%%LOOP_WITHOUT_CF:
    mov di, si
    add cx, 0       ; clear AF
    clc
    %1 di, cl
    DO_PRINT 0x70C4  ; PF, ZF, SF
    inc cl
    cmp cl, 66
    jne %%LOOP_WITHOUT_CF

    pop di
    ret

%endmacro

%macro ROTATE_MACRO 2
%defstr OP_NAME %2
%substr OP_NAME_1 OP_NAME 1
%substr OP_NAME_2 OP_NAME 2
%substr OP_NAME_3 OP_NAME 3

    push di
    mov si, di
    mov byte [OPSTR],   OP_NAME_1
    mov byte [OPSTR+1], OP_NAME_2
    mov byte [OPSTR+2], OP_NAME_3

    ; do single-bit variant of operation
    %1
    %2 di, 1
    mov cx, 1
    DO_PRINT 0x7801  ; CF, OF

    ; do shift-count-in-CL variant of operation
    xor cx, cx
%%LOOP:
    mov di, si
    %1
    %2 di, cl
    DO_PRINT 0x7001  ; CF
    inc cl
    cmp cl, 66
    jne %%LOOP

    pop di
    ret

%endmacro

DO_SHL:     SHIFT_MACRO SHL
DO_SHR:     SHIFT_MACRO SHR
DO_SAR:     SHIFT_MACRO SAR
DO_ROL:     ROTATE_MACRO CLC , ROL
DO_ROR:     ROTATE_MACRO CLC , ROR
DO_CLC_RCL: ROTATE_MACRO CLC , RCL
DO_CLC_RCR: ROTATE_MACRO CLC , RCR
DO_STC_RCL: ROTATE_MACRO STC , RCL
DO_STC_RCR: ROTATE_MACRO STC , RCR

%defstr TEST_OP_STR TEST_OP
STRING db "XXXX (XXXX) = XXXX "
OPSTR db "XXX XXXX"
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
