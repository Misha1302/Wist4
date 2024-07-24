#include <stdio.h>
#include <fstream>
#include <iterator>
#include <vector>
#include <string.h>
#include <iostream>
#include "Windows.h"

int main(int argc, char* argv[])
{
    char* path = (char*)R"(..\..\Wist\bin\Debug\net8.0\program.bin)";
    if (argc == 2) path = argv[0];

    std::ifstream input(path, std::ios::binary);
    std::vector<unsigned char> buffer(std::istreambuf_iterator<char>(input), {});

    if (buffer.size() == 0) return 1;

    unsigned char* p = (unsigned char*)VirtualAlloc(NULL, buffer.size(), MEM_COMMIT, PAGE_EXECUTE_READWRITE);
    if (p == NULL) return 2;

    for (int i = 0; i < buffer.size(); i++)
        p[i] = buffer[i];

    long long (*ptr_method)() = (long long(*)())p;
start_of_program:
    long long result = (*ptr_method)();
    std::cout << ">>>" << result << "\n";
}