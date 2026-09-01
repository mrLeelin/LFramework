#include "il2cpp-config.h"
#include "vm/InternalCalls.h"
#include "vm/Runtime.h"
#include "utils/Il2CppHashMap.h"

#include "hybridclr/CommonDef.h"

#include <map>
#include <string>
#include <cstring>

typedef Il2CppNotDefaultKeyHashMap<const char*, Il2CppMethodPointer, hybridclr::CStringHash, hybridclr::CStringEqualTo> ICallMap;
static ICallMap s_InternalCalls;

namespace il2cpp
{
namespace vm
{
    void InternalCalls::Add(const char* name, Il2CppMethodPointer method)
    {
        //ICallMap::iterator res = s_InternalCalls.find(name);

        // TODO: Don't assert right now because Unity adds some icalls multiple times.
        //if (res != icalls.end())
        //  IL2CPP_ASSERT(0 && "Adding internal call twice!");

        IL2CPP_ASSERT(method);

        s_InternalCalls[name] = method;
    }

    Il2CppMethodPointer InternalCalls::Resolve(const char* name)
    {
        // Try to find the whole name first, then search using just type::method
        // if parameters were passed
        // ex: First, System.Foo::Bar(System.Int32)
        // Then, System.Foo::Bar
        ICallMap::iterator res = s_InternalCalls.find(name);

        if (res != s_InternalCalls.end())
            return res->second;

        //std::string shortName(name);
        const char* pos = std::strchr(name, '(');

        if (pos)
        {
			std::string shortName(name, pos - name);
            res = s_InternalCalls.find(shortName.c_str());

            if (res != s_InternalCalls.end())
                return res->second;
        }

        return NULL;
    }
} /* namespace vm */
} /* namespace il2cpp */
