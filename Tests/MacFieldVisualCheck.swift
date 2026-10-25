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
}
print("MAC_FIELD_VISUAL_CHECK_PASSED")
