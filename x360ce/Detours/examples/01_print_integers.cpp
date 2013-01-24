#include <iostream>
#include "../detours.h"

typedef int (*tPrintIntegers)(int, int);

MologieDetours::Detour<tPrintIntegers>* detour_PrintIntegers = NULL;
MologieDetours::Detour<tPrintIntegers>* detour2_PrintIntegers = NULL;

int hook_PrintIntegers(int param1, int param2)
{
	param1 <<= 1;
	param2 <<= 1;
	
	int two = detour_PrintIntegers->GetOriginalFunction()(param1, param2);
	return two + 1;
}

int hook2_PrintIntegers(int param1, int param2)
{
	param1 <<= 1;
	param2 <<= 1;
	
	// Issue 1: A detour library which does not relocate code would get you to the unrelocated
	// jmp hook_PrintIntegers from detour_PrintIntegers now. This would result in a crash.
	int two = detour2_PrintIntegers->GetOriginalFunction()(param1, param2);
	return two + 2;
}

int PrintIntegers(int param1, int param2)
{
	std::cout << "Param1: " << param1 << ", Param2: " << param2 << std::endl;
	return 2;
}

int main(int argc, char* argv[])
{
	// Prints "Param1: 1, Param2: 2", returns 2
	PrintIntegers(1, 2);
	
	// Detour PrintIntegers
	try
	{
		detour_PrintIntegers = new MologieDetours::Detour<tPrintIntegers>(PrintIntegers, hook_PrintIntegers);
		detour2_PrintIntegers = new MologieDetours::Detour<tPrintIntegers>(PrintIntegers, hook2_PrintIntegers);
	}
	catch(MologieDetours::DetourException &e)
	{
		// Something went wrong
		std::cerr << e.what() << std::endl;
		return 1;
	}
	
	// Prints "Param1: 4, Param2: 8", returns 5
	PrintIntegers(1, 2);
	
	// Issue 2: Every detour library I have tested before would break the callchain at this point.
	// The next call to PrintIntegers would crash the application.
	delete detour_PrintIntegers;
	
	// Prints "Param1: 2, Param2: 4", returns 3
	PrintIntegers(1, 2);
	
	// Remove the second hook. Usually, this hook would have to be removed first.
	delete detour2_PrintIntegers;
	
	// Prints "Param1: 1, Param2: 2", returns 2
	PrintIntegers(1, 2);
	
	// Huge success!
	return 0;
}