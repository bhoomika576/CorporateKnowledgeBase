def greet(Lang):
    if lang == 'es':
        message = "hola"
        return message
    elif lang == 'fr':
         message ="bonjour"
         return message
    else :
        message = "hello"
        return message

print(greet('fr'))