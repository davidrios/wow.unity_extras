import argparse
import csv
import sys
from pathlib import Path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("source_dir")
    parser.add_argument("--dest_dir")

    args = parser.parse_args()

    source_dir = Path(args.source_dir)
    dest_dir = Path(args.dest_dir) if args.dest_dir else None

    if not source_dir.is_dir() or (dest_dir is not None and not dest_dir.is_dir()):
        sys.exit("Invalid source or dest dir.")

    all_vals = set()

    for csv_file in source_dir.glob('*_ModelPlacementInformation.csv'):
        wrt = None
        if dest_dir is not None:
            dest_csv = dest_dir / csv_file.relative_to(source_dir)
            wrt = dest_csv.open('wb')

        rdr = csv.reader(csv_file.open('r'), delimiter=';')
        next(rdr) # skip header
        for line in rdr:
            if not line or len([a for a in line if a]) == 0:
                continue
            
            line = tuple(line)
            if line in all_vals:
                print('duplicate!', line)
                continue

            all_vals.add(line)
            if wrt is not None:
                wrt.write((';'.join(line) + '\n').encode('utf8'))


if __name__ == '__main__':
    main()