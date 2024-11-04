def conversion(currency):
    if conversion == 'dollar':
        userinput = float(input("enter USD value"))
        result = userinput*46
        return result
    elif conversion == 'euro':
         userinput = float(input("enter EURO value"))
         result = userinput*50
         return result
    else :
        userinput = float(input("enter POUNDS value"))
        result = userinput*60
        return result


print("enter USD or POUND or EUR to convert to Mauritian Rupees")
conversion()    