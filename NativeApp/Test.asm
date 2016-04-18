	AREA my_test, CODE, READONLY
	EXPORT test

test proc
	mov r0, #1
	mov pc, lr
	endp

	end