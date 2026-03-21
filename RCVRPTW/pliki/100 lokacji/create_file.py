import sys
import os

def truncate_file(filename, n):
    with open(filename, 'r') as f:
        lines = f.readlines()

    # pierwsza linia to nagłówek
    header = lines[0]
    
    # kolejne linie to dane
    data = lines[1:n+1]

    # nowa nazwa pliku
    base, ext = os.path.splitext(filename)
    new_filename = f"{base}_{n}{ext}"

    with open(new_filename, 'w') as f:
        f.write(header)
        f.writelines(data)

    print(f"Saved: {new_filename}")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: python script.py <filename> <n>")
        sys.exit(1)

    filename = sys.argv[1]
    n = int(sys.argv[2])

    truncate_file(filename, n)