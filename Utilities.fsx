module Utilities


open System

let getRandomNum(l: int, u: int) =
    let r = Random()
    r.Next(l, u+1)

let ranStr(n: int) = 
    let r = Random()
    let chars = Array.concat([[|'a' .. 'z'|];[|'A' .. 'Z'|];[|'0' .. '9'|]])
    let sz = Array.length chars in
    String(Array.init n (fun _ -> chars.[r.Next sz]))

let getRandomElement(arr: list<string>) =
    let rnd = Random();
    if arr.Length = 0 then
        ""
    else
        arr |> Seq.item (rnd.Next arr.Length)

let getRandomIntegerElement(arr: list<int>) =
    let rnd = Random();
    if arr.Length = 0 then
        -1
    else
        arr |> Seq.item (rnd.Next arr.Length)

let shouldMention() =
    let rnd = Random();
    let flip = rnd.Next 2
    if flip >= 1 then
        true
    else
        false