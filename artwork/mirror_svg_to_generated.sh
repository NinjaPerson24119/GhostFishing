#!/usr/bin/env bash

src_dir="svg"
dest_dir="generated"

rm -rf $dest_dir
mkdir $dest_dir

process_file() {
    local src_file="$1"
    if [[ ! $src_file =~ \.svg$ ]]; then
        echo "Skipping: $src_file: not an SVG file"
        return
    fi

    local dest_file="$dest_dir${src_file#$src_dir}"
    local dest_file_svg="$(echo "$dest_file" | sed 's/\.svg$/.png/')"
    echo "Converting: $src_file -> $dest_file_svg"

    mkdir -p $(dirname $dest_file_svg)
    inkscape --export-type="png" -o "$dest_file_svg" "$src_file"

    echo "Converted: $src_file -> $dest_file"
}

file_list=$(find "$src_dir" -type f)
for file in $file_list; do
    process_file "$file"
done
