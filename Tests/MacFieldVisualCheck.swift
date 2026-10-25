import AppKit
import Foundation

// The smoke fixture uses the grass field: a solid white half is a broken overlay.
for argument in CommandLine.arguments.dropFirst() {
    guard let data=NSData(contentsOfFile:argument),let bitmap=NSBitmapImageRep(data:data as Data) else {
        fputs("Missing duel screenshot: \(argument)\n",stderr);exit(1)
    }
    var ratios:[Double]=[]
    for band in [(0.15,0.40),(0.60,0.85)] {
        var white=0,total=0
        for y in stride(from:Int(Double(bitmap.pixelsHigh)*band.0),to:Int(Double(bitmap.pixelsHigh)*band.1),by:4) {
            for x in stride(from:Int(Double(bitmap.pixelsWide)*0.25),to:Int(Double(bitmap.pixelsWide)*0.75),by:4) {
                if let color=bitmap.colorAt(x:x,y:y)?.usingColorSpace(.deviceRGB) {
                    total+=1
                    if color.redComponent>0.96 && color.greenComponent>0.96 && color.blueComponent>0.96 {white+=1}
                }
            }
        }
        ratios.append(Double(white)/Double(max(1,total)))
    }
    print("MAC_FIELD_WHITE_FRACTIONS",argument,ratios)
    if ratios.contains(where:{$0>0.45}) {fputs("Solid-white field overlay detected.\n",stderr);exit(1)}
    // The fixed grass-field fixture also exposes broken Intel mate and grave
    // materials: white silhouettes and large, completely black grave meshes.
    for (x0,y0,x1,y1,checkWhite) in [(0.852,0.194,0.961,0.389,true),(0.020,0.444,0.129,0.639,true),(0.188,0.153,0.273,0.389,false),(0.758,0.528,0.844,0.764,false)] {
        var failed=0,total=0
        for y in stride(from:Int(Double(bitmap.pixelsHigh)*y0),to:Int(Double(bitmap.pixelsHigh)*y1),by:2) {
            for x in stride(from:Int(Double(bitmap.pixelsWide)*x0),to:Int(Double(bitmap.pixelsWide)*x1),by:2) {
                if let color=bitmap.colorAt(x:x,y:y)?.usingColorSpace(.deviceRGB) {
                    total+=1
                    let rgb=[color.redComponent,color.greenComponent,color.blueComponent]
                    if checkWhite ? rgb.allSatisfy({$0>0.96}) : rgb.allSatisfy({$0<0.04}) {failed+=1}
                }
            }
        }
        let ratio=Double(failed)/Double(max(1,total))
        print("MAC_MODEL_INVALID_FRACTION",checkWhite ? "mate" : "grave",ratio)
        if ratio > (checkWhite ? 0.15 : 0.35) {fputs("Untextured mate or grave model detected.\n",stderr);exit(1)}
    }
}
print("MAC_FIELD_VISUAL_CHECK_PASSED")
