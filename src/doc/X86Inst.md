
SOME 8086 INSTRUCTIONS
======================

Arithmetic Instructions I:
--------------------------

- Instructions: ADD, ADC, SUB, SBB, CMP, INC, DEC.  Implemented in __CpuArith1.cs__ and __CpuArith2.cs__.

| Instruction | Result (R) | Carry Flag (CF) | Adjust Flag (AF) | Overflow Flag (OF) |
| ----------- | ---------- | --------------- | ---------------- | -------------------|
| ADD X, Y | X + Y | Carry out of high bit   | Carry out of bit 3 | Sign of R opposite to both X and Y|
| SUB X, Y | X - Y | Borrow into low bit     | Borrow into bit 3  | Sign of X opposite to both Y and R|
| AND X, Y | X & Y      | Clear        | Not modified             | Clear |
| OR  X, Y | X &#124; Y | Clear        | Not modified             | Clear |
| XOR X, Y | X ^ Y      | Clear        | Not modified             | Clear |
| INC X    | X + 1      | Not modified | Carry out of bit 3       | Signed positive X to negative R |
| DEC X    | X - 1      | Not modified | Borrow into bit 3        | Signed negative X to positive R |

- ADC is same as ADD (X + (CarryFlag ? 1 : 0)), Y

- SBB is same as SUB (X - (CarryFlag ? 1 : 0)), Y

- CMP is same as SUB without storing the result.

- Sign Flag (SF) is set to the sign bit of the result.

- Zero Flag (ZF) is set if the result is zero, otherwise clear.

- Parity Flag (PF) is if the result has an even number of bits set; clear if the result has an odd number of bits set.

Arithmetic Instructions II:
---------------------------

- Instructions: NEG, MUL, IMUL, DIV, IDIV.  Implemented in __CpuArith2.cs__.

| Width   | Instruction | Result          | Carry Flag (CF)          | Overflow Flag (OF)    |
| ------- | ----------- | ------------------------ | --------------------- |
| 8-bit   | NEG X       | R = - X         | Set if R < 0             | Set if R = X = 0x80   |
| 16-bit  | NEG X       | R = - X         | Set if R < 0             | Set if R = X = 0x8000 |

- NEG does not negate if the operand equals MinValue
    - MinValue is 0x80 for an 8-bit operand, and 0x8000 for a 16-bit operand.
    - In this case the Overflow Flag (OF) is set; otherwise it is clear.


- Sign Flag (SF) is set to the sign bit of the result.

- Zero Flag (ZF) is set if the result is zero, otherwise clear.

- Parity Flag (PF) is if the result has an even number of bits set; clear if the result has an odd number of bits set.


| Width   | Instruction | Result          | Carry Flag (CF) and Overflow Flag (OF)    |
| ------- | ----------- | --------------- | ----------------------------------------- |
| 8-bit   | MUL  X      | AH:AL = X * AL  | Set if AH != 0        |
| 8-bit   | IMUL X      | AH:AL = X * AL  | Set if AH != Sign(AL) |
| 16-bit  | MUL  X      | DX:AX = X * AX  | Set if DX != 0        |
| 16-bit  | IMUL X      | DX:AX = X * AX  | Set if DX != Sign(AX) |

- 8-bit (I)MUL stores 16-bit product in AX (AH:AL).

- 16-bit (I)MUL stores 32-bit product in DX:AX.

- Carry Flag (CF) and Overflow Flag (OF) indicate if the upper half of the result (AH or DX) is significant.
    - For MUL - unsigned multiplication:
        - Set if the upper half of the result (AH or DX) is non-zero; otherwise cleared.
    - For IMUL - signed multiplication:
        - Sign-extend the lower half of the result (AL or AX) into an intermediate result R'.
        - Compare against 16-bit result in AX or 32-bit result in DX:AX.
        - If equals, CF and OF are cleared; otherwise set.


- Adjust Flag (AF), Sign Flag (SF), Zero Flag (ZF), and Parity Flag (PF) are undefined.

| Width   | Instruction | Quotient Result | Reminder Result |
| ------- | ----------- | ----------------| ----------------|
| 8-bit   | (I)DIV  X   | AL = AX / X     | AH = AX % X     |
| 16-bit  | (I)DIV  X   | AX = DX:AX / X  | DX = DX:AX % X  |

- 8-bit (I)DIV stores quotient in AL, reminder in AH.

- 16-bit (I)DIV stores quotient in AX, reminder in DX.

- Uses unsigned division and modulus for DIV.  Uses signed division and modulus for IDIV.

- Interrupt 0 (divide error) is generated:
    - If the operand is zero.
    - If the quotient result cannot fit in AL (for 8-bit division) or AX (for 16-bit division).
        - For DIV, when the quotient is greater than 0xFF or 0xFFFF.
        - For IDIV, when the quotient is greater than 0x7F or 0x7FFF.
        - For IDIV, when the quotient is smaller than 0x80 or 0x8000.

- Carry Flag (CF), Overflow Flag (OF), Adjust Flag (AF), Sign Flag (SF), Zero Flag (ZF), and Parity Flag (PF) are undefined.

Logical Instructions:
---------------------

- Instructions: AND, OR, XOR, TEST, NOT.  Implemented in __CpuArith1.cs__.

- Logical AND, OR, XOR with operands X, Y.

- TEST is same as AND without storing the result.

- Carry Flag (CF) and Overflow Flag (OF) are cleared.

- Sign Flag (SF) is set to the sign bit of the result.

- Zero Flag (ZF) is set if the result is zero, otherwise clear.

- Parity Flag (PF) is if the result has an even number of bits set; clear if the result has an odd number of bits set.

- Adjust Flag (AF) is not modified.

- NOT can be (and is) implemented as XOR with -1.
    - The NOT instruction does not modify any flags.

| Instruction | Result (R) | Carry Flag (CF) | Adjust Flag (AF) | Overflow Flag (OF) |
| ----------- | ---------- | --------------- | ---------------- | -------------------|
| AND X, Y | X &#38; Y | Carry out of high bit   | Carry out of bit 3 | Sign of R opposite to both X and Y|

Shift and Rotate Instructions:
------------------------------

| Instruction | Result (R) | Carry Flag (CF)      | Overflow (OF) (when N = 1)       |
| ----------- | ---------- | -------------------- | ---------------------------------|
| SHL X, N    | Shift left:<br>X << N     | Last bit shifted out | Sign of R != last bit shifted out |
| SHR X, N    | Unsigned shift right:<br> X >>> N    | Last bit shifted out | Sign of X |
| SAR X, N    | Signed shift right:<br>X >> N     | Last bit shifted out | Zero |
| ROL X, N    | Rotate left:<br>(X << N) &#124; (X >> (W - N)) | Low bit of R | Sign of R != low bit of R |
| ROR X, N    | Rotate right:<br>(X >> N) &#124; (X << (W - N)) | Sign of R | Sign of R != next highest bit of R |
| RCL X, N    | Rotate left with carry:<br>(((X << 1) &#124; CF) << N) &#124; (X >> (W - N)) | Sign of R | Sign of R != low bit of R |
| RCR X, N    | Rotate right with carry:<br>((X &#124; (CF << W)) >> (N + 1)) &#124; (X << (W - N)) | Low bit of R | Sign of X != original Carry Flag |

- SAL X, N (Shift Arithmetic Left) is same as SHL X, N.

- __N__ is the number of bits to shift or rotate.  No operation occurs if __N__ is zero.

- __W__ is the width of the operand:  8 or 16.

- The Overflow Flag is only set as described in the table above if __N__ = 1.  For other counts, it is undefined.

- Shift Instructions only:

    - Unlike later processors, the 8086 does not mask __N__ to 5 or 6 bits.
    - If shift count is equal or larger than the operand size:
        - The result is all bits clear (= 0), except:
        - The result is all bits set (= -1) for a signed shift right (SAR) with a negative __X__.
        - The Carry Flag is undefined.
    - Sign Flag, Zero Flag and Parity Flag are set as per Arithmetic Instructions (see above).
    - Adjust Flag is not modified.

- Rotate instructions only:

    - While the 8086 does not mask __N__, it can (and should) be constrained:
        - __N__ = __N__ AND (__W__ - 1) for ROL and ROR.
        - __N__ = (__N__ - 1) MOD (__W__ + 1) for RCL and RCR.
    - Adjust Flag, Sign Flag, Zero Flag and Parity Flag are not modified.

- Implemented in __CpuArith2.cs__.
