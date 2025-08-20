cmake_minimum_required(VERSION 3.10)

project(cLox)

file(GLOB SOURCES "*.c")

add_executable(cLox ${SOURCES})

set_property(TARGET cLox PROPERTY C_STANDARD 11)