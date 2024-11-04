def greet(Lang):
    if lang == 'es':
        return 'hola'
    elif lang == 'fr':
        return 'bonjour'
    else :
        return 'hello'

print(greet(fr))