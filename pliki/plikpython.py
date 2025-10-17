import os

def list_files_in_directory(directory):
    """
    Wypisuje wszystkie nazwy plików w katalogu w formacie "nazwa_katalogu/plik",
    oddzielone przecinkami.
    
    :param directory: Ścieżka do katalogu.
    """
    try:
        # Lista do przechowywania ścieżek plików
        files = []

        # Iteracja po plikach w katalogu
        for filename in os.listdir(directory):
            file_path = os.path.join(directory, filename)
            
            # Sprawdzenie, czy jest to plik
            if os.path.isfile(file_path):
                files.append(f'"{directory}/{filename}"')
        
        # Wypisanie listy plików oddzielonych przecinkami
        print(", ".join(files))
    except Exception as e:
        print(f"Błąd: {e}")

# Wywołanie funkcji z katalogiem podanym jako argument
directory_path = "200 lokacji"  # Zamień na odpowiednią ścieżkę
list_files_in_directory(directory_path)
