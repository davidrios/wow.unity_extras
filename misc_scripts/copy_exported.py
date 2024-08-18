import argparse
import re
import shutil
import sys
from pathlib import Path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("source_dir")
    parser.add_argument("dest_dir")
    parser.add_argument("--fbx-dir")
    parser.add_argument(
        "--replace", action="store_true", help="Replace all files in destination."
    )
    parser.add_argument(
        "--dry-run", action="store_true", help="Don't do the actual copying."
    )
    parser.add_argument(
        "--anims", action="store_true", help="Copy FBX with animations."
    )

    args = parser.parse_args()

    source_dir = Path(args.source_dir)
    dest_dir = Path(args.dest_dir)

    if not source_dir.is_dir() or not dest_dir.is_dir():
        sys.exit("Invalid source or dest dir.")

    def copy_to(source: Path, dest: Path, force_replace=False):
        if not source.is_file():
            return

        dest.parent.mkdir(parents=True, exist_ok=True)
        if args.replace or force_replace or not dest.exists():
            print(source, "->", dest)
            if not args.dry_run:
                shutil.copy(source, dest)
        else:
            print("[!]", source, "-", dest)

    fbx_dir = Path(args.fbx_dir) if args.fbx_dir is not None else None
    if fbx_dir is not None and not fbx_dir.is_dir():
        fbx_dir = None

    fbx_extras = []

    for source_item in source_dir.glob("**/*"):
        if source_item.suffix == ".bin":
            continue

        relative_source_item = source_item.relative_to(source_dir)
        dest_file = dest_dir / relative_source_item

        if source_item.suffix == ".gltf":
            if fbx_dir is None:
                continue

            source_fbx = fbx_dir / relative_source_item.with_suffix(".fbx")
            if not source_fbx.is_file():
                continue

            fbx_extras.append(
                (
                    source_fbx,
                    dest_dir / source_fbx.relative_to(fbx_dir),
                )
            )

            extra_source = source_fbx.with_name(
                source_fbx.name.replace(".fbx", "_anim.fbx")
            )
            if args.anims and extra_source.is_file():
                fbx_extras.append(
                    (extra_source, dest_file.with_name(extra_source.name), True)
                )

            for extra_file in source_fbx.parent.glob('*.*'):
                if not re.match(r'.+\.(png|json)$', extra_file.name):
                    continue

                fbx_extras.append(
                    (
                        extra_file,
                        dest_dir / extra_file.relative_to(fbx_dir),
                        extra_file.suffix == '.json',
                    )
                )

            continue

        copy_to(
            source_item,
            dest_file,
            force_replace=str(relative_source_item).startswith("creature_data"),
        )

    # copy fbx stuff last
    for extra in fbx_extras:
        copy_to(*extra)


if __name__ == "__main__":
    main()
