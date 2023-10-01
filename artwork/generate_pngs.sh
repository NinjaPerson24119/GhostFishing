#!/usr/bin/env bash

src_dir="svg"
dest_dir="generated"

rm -rf $dest_dir
mkdir $dest_dir

process_file() {
    local src_file="$1"
    local dest_file="$dest_dir/${src_file#$src_dir/}"
    local dest_file_svg=$(echo "$dest_file" | sed 's/\.svg$/.png/')

    mkdir -p "${dirname "$dest_file_svg"}"
    inkscape --export-type="png" -o "$dest_file_svg" "$src_file"

    echo "Converted: $src_file -> $dest_file"
}

find "$src_dir" -type f -exec bash -c 'process_file "$0"' {} \;
