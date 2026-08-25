#pragma once

#include "il2cpp-runtime-metadata.h"

namespace il2cpp
{
namespace metadata
{
    class Il2CppMethodSpecOrGenericMethod
    {
    public:
        Il2CppMethodSpecOrGenericMethod() : storage(0) {}
        Il2CppMethodSpecOrGenericMethod(const Il2CppGenericMethod* gmethod);
        Il2CppMethodSpecOrGenericMethod(const Il2CppMethodSpec* methodSpec);
        Il2CppGenericMethodKey BuildGenericMethodKey() const;
        bool IsNull() const { return (storage & ~(uintptr_t)1) == 0; }

    private:
        uintptr_t storage;
    };
}
}
