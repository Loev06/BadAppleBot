﻿using ChessChallenge.API;

/**
 * BadAppleBot, by Loev06
 *
 * Made for the Chess Coding Challenge by Sebastian Lague.
 * https://github.com/SebLague/Chess-Challenge
 *
 * Next to making a serious bot (LoevBot), I wanted to do make something creative. I was looking through the
 * documentation, and found the function BitboardHelper.VisualizeBitboard(ulong bitboard).
 *
 * After some messing about I decided to make a video decompressor.
 * This program plays the first 121 seconds of the YouTube video Bad Apple with 30 fps:
 * https://www.youtube.com/watch?v=FtutLA63Cp8
 *
 * Using an external Python script, I captured 64 pixels for every frame and compressed it using a made-up
 * compression algortithm, so I could fit it within the token limit for this challenge.
 *
 * The original 121 seconds with 30 fps consist of 121 * 30 * 64 = 232.320 bits being displayed.
 * This gets compressed to 52.873 bits, so it fits in 827 ulongs = 827 tokens.
 * It is lossless (meaning the original 64-bit capture gets displayed exactly the same),
 * and has a compression rate of 52873 / 232320 = 22.76%, which I'm quite proud of, considering I had no prior
 * experience regarding video compression, and the fact that the decompressor must fit within the token limit.
 *
 * The data gets compressed as follows:
 * The ulong[] data should be seen as one continuous stream of words. Which consists of 64 blocks of data, one block per pixel:
 *   The first 3-bit word of every block indicates the word size of every consecutive word in the block:
 *     WordSize = 4 + the 3-bit word, meaning it ranges from 4 to 11.
 *
 *   Every consecutive word of size WordSize contains the amount of frames it takes before that pixel switches color. This explains
 *   the use of differing word sizes: the middle pixels switch color more often, meaning the maximium time before a pixel switches
 *   color is shorter, which allows for shorter words and thus more compression.
 *
 *   One extra trick is used:
 *   When the amount of frames between two switches is greater than the wordSize allows, we use a word containing wordSize zeroes.
 *   in that case the program reads the next words until there is a non-zero value. The time between color switch then becomes:
 *   (2 ^ wordSize - 1) * amount of zero-words + the value of the non-zero word
 *   This trick allows for longer frame counts between color switches on occasion, without having to raise the overall wordSize of the block.
 */

public class MyBot : IChessBot {
	ulong[] diffs = new ulong[3630], data = {0x604021cdfb00020b,0xff002000d,0x2621400020c0fc01,0x2100ce84ec04b86,0xc0a03e5101440701,0xf84000001c598da1,0x103b02aa00002cb4,0x803104c583020c10,0x45044434a071809,0x58604921e24f0104,0x95038c013231580,0x100035a301012f60,0xf40378000798c3b6,0x2d04187930004181,0x5d811400d0d08002,0x12c810237d,0x7240ce2603c42845,0x6830b9c2c9000460,0x830114bd0861638,0x1e20c89081052504,0xf1737c5803e08530,0x72c0128175061630,0x895c28002080ae14,0x1d8803bd0000bc3,0xc08f0900082042c0,0x2001013808f80495,0x2300050616797f97,0x686138c003022050,0xb084a800c82cca5,0xab045d123c59002,0x812090900c48c04,0x10117e216a80600a,0x5ac0002562cf5202,0x40209c19a4540af,0x20712083940e04f,0x2e6b467e2d548302,0x4de9050d02080820,0x8050269035261c15,0x586d50600481ee05,0x58082032e742cd1c,0xde0208a0c040b800,0x8e89542ae80301c0,0x8642063400510106,0xec00c01826748861,0x6882061038115801,0x2820c78182167,0x3ac19b808408da10,0x8104051eba35c38,0x9404041010c04,0x404eb2040ced218,0x814840880420405,0xae9a2b484801207a,0xa700004808193046,0x116120840a7ac020,0x30c16b46900db254,0xc408104080830230,0x1024819020810104,0x3900541803638f8f,0x18514203863e84,0xa40d51041018910,0x12d7009b420002a,0xc1313d392820c0b0,0xe9230290143850e1,0x1020428102080813,0xba44c91e025edc08,0x24009036810cb616,0x3c081260054da728,0x82b65ee8414dc006,0x98912ab88600b00,0xa247091435186240,0x5089059200685881,0x20835806b608fc80,0x1406ab088008,0x9b1a4a0b0001a0f4,0x821983156b2d80,0x880821020bac3e00,0x78431c8000c8f020,0xa3ba00da03e14d05,0x128238e58260258,0x85300028672a2c0,0x5980186280000332,0xbb8049388a8200,0x41c43332e29660e0,0x631e29c5164b001e,0x802841060ae10c10,0x303b80e2001512b9,0x804bd2344818b12,0xc800d03e600340ae,0xc2870c343531c615,0x6040ad000818140a,0x502ec485020421b0,0xf702042a50e5436,0xf093f00000920234,0x10a81048c01514,0x1d04f02851876bd0,0x97ab081800f0400b,0x6189161e1ea837c0,0x96fa0003f6010830,0xc000010af7f04cc6,0x2e010304081fb00f,0x31e3c3005a041650,0x1914449112062848,0x20000007c819ce99,0xb400030c0030b6f,0x83085c50402081ed,0x8e860e0c11d421a3,0xce44601b28241,0xd502001b21c5ac80,0x1d1cc00038be4,0x2600bc0408101064,0x561038046082c80,0x5be39ac083021220,0x41080a011000000,0x8a0151e0c0428604,0x82262c082040f484,0x458d045dba408140,0x44c29e0414182022,0x8594c002027011f3,0xc6911674c1228108,0x2280832b58098116,0x8a60800102099010,0x6819806995280974,0x5cf030c09707aa08,0x4080821c000ca10,0x43f401822406001,0x8001263008d0e300,0x43400f7e2011fead,0xb22003c616281820,0x1849000684b00018,0x1819a19b438d184f,0x2a801811e4c2932c,0x6a800fa02a400100,0x2c0104e5d3002083,0x448c461dcb4000,0x8b0f00c1787ed75,0x709a1460e640014a,0x1100604100218e04,0x840420420410820,0x20dc02d0030936c2,0x512c60013c20812c,0x60f10f606c08d1,0x1105dd251531be36,0x2b11d7c604108,0x110703d1408b0b30,0xf09c24f0000130d4,0xc607f18e00b34cad,0x6cb04c51614d5c51,0x9800016411e00080,0x82d716c841064844,0x2844a0392184,0x8409e3e204004200,0x3a0ba1a270077e3,0x31040847b400a088,0x8208208210208408,0x8880140016c1840,0xe94c90210a83c104,0x941941ae32a40f58,0x6600c19411a0941,0x6102e0e09e03ec50,0x9800004810480,0x9873340880dca,0xc49181c615580b80,0xa8d8e51ad0b3a4b8,0x4124244040364061,0x1800031cb59d78a1,0x44964800c4085045,0x4011ae81880000c8,0x3403c11ca093c407,0xa00228004a501c1e,0x4104104108105f88,0xf310c1cc10410410,0x428f0ba0c746d0a2,0xca1081cc68004108,0x14908b0ca1081490,0x29c00184f0b32002,0x7100d0039401dc08,0x57703d0263459402,0x410134001220c384,0xc30898c83089c208,0x6321e00f098a0191,0x380b2803e27b00,0x21a0000b5b0b2000,0xb6b66902005c7922,0x8223a202ac00000b,0x2de0fd9820840820,0xa0839804cd284,0xaa48890001220910,0xeb0414c000b8ff,0xe08408c400008000,0x3428c0864cc08450,0xf5340e600021480e,0x449c3685a04a668c,0x8238a09008818e5c,0xb24040ae60040,0x2041036256992140,0x89201a09ba004811,0x9e56cdf82082102,0x408436081026600,0xa6580a004c18c04,0x21418c03cc104,0x8123005c34227180,0x330383e534006220,0x586d000b14007a20,0x10110647120a0c00,0x6048ff0001eb0084,0x40807e01700408b0,0xc053290000860815,0x6c20206041fb0190,0x1a1c32c165450c0a,0x8a023c0e30ca1784,0x4182027c7261c1c4,0x64105a4a881031a0,0x4741a93064000006,0x1ee09000060e1878,0x9c079c101181418,0x1602a87261500410,0x6830242b81460004,0xd250794001380204,0x180419c0c1aa8028,0x324495a814584,0xcf02060810204049,0x40b9a6d0083021a0,0xf16a84c00a052010,0x325be28281045c22,0x2c00120000098004,0x140d01f102448ffa,0x42e28184d412100,0x82890a9c18239002,0x3438604c0514089,0xaf83126bd0198020,0x222027ae002d91cc,0x3840cce80000009b,0xc30c482e8ec00928,0x805a208c69030c40,0xba00e1ba09622a0,0x40853023000c2082,0x185a818410410408,0x10c78420298210a1,0xa1ca01610813e596,0x5801600000a0ae25,0x8825800262005837,0xe00250800052000e,0x987400b8659,0xc2aa60414c0084,0xa1a8a0e0840d83c2,0xb9e30618c20462,0x1815eff3b058c001,0xd8104f0411013ab4,0x31c13c8b0ec00002,0x474423417d550040,0x911442850c60816,0x72835c42c218d000,0x412c404804904907,0x81042064097dce1,0x4013603081081042,0x8119b05600255a03,0x705d9e5e21081a24,0x23c613240381340,0x84051435d8304462,0xb07819a408c0000,0xc6c55b520d000018,0xcc14827f04b00834,0x40008068b89710f3,0xe69002053a25c29,0xa031e9acaa83163,0x17000000e1ce386,0x884954012002cc,0x18e706c0678cc820,0x110408e0940900d8,0x821021020c221cad,0x50003d840820820,0x4304b121a64a49c2,0x11b8c30e600970a0,0x205022046e12000e,0x4104082082082206,0xe146c8009600004,0x2a0001fac876242,0x8792b043006907d0,0x840b4d08616d8c7a,0xe4a15d27c040344,0x3021000c30865400,0x667a21900000d014,0xc44c51570012df00,0xb2d10832510f0c42,0x108105f7ce0039c0,0x46e4104104104104,0x5027cc1c910a05a0,0x21030c85c038527d,0x53c00c800104504,0x5405ca040f230cd0,0x8083041f41142182,0x2000ad005efb0029,0x488214ec0d00a151,0x90c3095000401082,0x8068318136218e12,0x8270417e03c35e,0x9ca2400210a1b404,0xc724ae0000221d80,0x63982f9263180004,0x380023802315c820,0xa822022020ba104a,0xe00f14f406200107,0x4dd80003c802843e,0x42808530002848a0,0xe00118c30c509f00,0x209519640a08430,0x1026c6044c983,0x868708850c05011c,0xa20444832026c11,0x422b600608448c1,0x2b4882b2b3c00130,0x4800d4754002853b,0x18c262079136814,0x80409b055802b0e5,0x1062518314041010,0x10a0240084046808,0xe831560108051038,0x8c57b00033810d03,0xf02091802410086,0x5f810204c6d9c003,0x2d5980104400a4c0,0x10bd420308401820,0x1e300841830c6c69,0x4046010440080000,0xf65d600015c0000,0x81b3240d82100690,0xa838c04a08062e14,0x4e3040b362ed0632,0x3828829a34e3130e,0x84042dba900000,0x22c3bd0e0c0865e1,0x93c27b8478005831,0x30e14801880c9865,0xf5c0a50001fb015c,0x12e000017c440831,0xe6b49098aa600eb,0x8101126018903a8,0x261020a040cc9708,0x8e09e50852c9888e,0x5553521d7035e163,0xe1479e00004c3022,0x13ecc48c0407f1f8,0x282302b883800078,0x2050f30084831318,0x4d43910c9018108,0x3e68e02002c08c,0x248419ca801e800,0x19828800000027c4,0x2384e0b0858aea1d,0x8bf8c98c40449019,0x40cc02c801996348,0x41040843a439a004,0xe18e98659020210,0x1d220617cf3bf2a7,0x83686102a00030,0xe00c20226889820,0x54c0b80003b8000a,0xd8a40298a08a018,0xce008300000b0718,0xb8433095400a2aa7,0xa5041244a821a304,0x1b448c0010041429,0xc224b1d2162d7704,0x4c14901960907440,0xe142101222000003,0xf25912955400e08,0x4814461481450451,0xe486455487006183,0x420441860860a204,0x415910013410833,0x304204108634a281,0x45dc2d7979cb80d8,0xa304108160f98c2,0x914e0da0083f8182,0x5e0421032306d3f8,0x980000084d466410,0x310620295d064942,0xd34d600a30000040,0x1081010b338e0071,0x68b0d045610f3cc,0xbe03425e1180008,0x2504084786020297,0x505f258648ac8890,0x2822000b3a036001,0x408240a48444f300,0x86a887870a600810,0x8a30a10a1be21e0,0x8294dbca92806684,0x1208210208229839,0x1220827cef390000,0xa40219a08e10c206,0x6a4896802da3daf1,0x3100f742e1836006,0x873c18208658f,0x629333a0c4b8000,0x8209a0040807042,0xadc400c5a3380180,0x43b22986362a430,0x9d983c900d1210a7,0x4120f0000c12402,0x370a4022d3095f02,0x1028f19402a0bb4c,0x5433c07401c10589,0x30304504c113101,0x12000c19836cd14,0x8f0816411a30012,0x508104104210a041,0xdbedd383041041d8,0x460891465002d67,0x53001045101141,0x17758194e582db00,0xd5c113d0c209f0,0xb115010e00305780,0x81607250e40d80a5,0x1cc14815d2540003,0x608020a148046225,0x82905a004000343,0x599908628a00188,0x82c32a1879002d48,0xa9010325d2800020,0x32014be305410110,0x613810a68433000,0x28c08608215a4863,0x6bce99640c204104,0x9348248068608d00,0xe5700000000648a1,0x20ca000a1410a926,0xf14d0001478208,0x3ac24299a1248a0,0x42efe2e06e00084e,0x611811c24e31418,0xfb000000010eb40,0x280451431064d40,0xe08314304e7802be,0xc42868a72c000064,0x240000a1c1065108,0x60c51016821c4044,0x930c1182041082c0,0xd04107b0a62912,0x4834000000090139,0x800132c30d91c821,0x5400000cc0410428,0x204107d19604b840,0x24900014421211,0x75120ae000560320,0x8174c00438c4,0x15800028400800,0x294089800000f800,0x700e5089c4070418,0x469c6a714a850a10,0x64002dd80766c168,0xc0b1108488741db,0x4251ed401bc82720,0xf020841804081010,0x40a70001fb015430,0xad2a0000500821f5,0x80001683fe002402,0x9020374189021180,0xb43b30e253800a2c,0x8a08705d4c812f0c,0x1012003810cbc00a,0x2cf382010c0a082,0x67c4f106f4030711,0x90428186020878ba,0x72014b11040858,0x7e800e2f15805a05,0x40220206020c7540,0x2bd0010f0b084a80,0x9395465c58c0605,0xa2091010404b0232,0x88030020d561d301,0x13709ac2aa04280,0x364eaf06444e4021,0x86a043887d200030,0xf002264016a40848,0x90d060c08a560211,0x2605e9070c01705,0x90e0cd020a54007,0x800c9c0990e4c2,0x5e0000750d01fe00,0x118c28c88333388f,0x2095c07740664183,0x4631b6441ec0003d,0x201608a343816101,0x844f08c2c0b0a64,0x4341048120426,0x115a7718e00844,0xd800886d31cc138d,0x4235007848423cb,0xe0c0059000023c6c,0x3102452940000d2a,0x241ecef7829810f7,0x8410b4a471d48014,0x4dc20e4010528100,0x402c41186041,0x1800600f201f800,0xcf718008848529ce,0x4d260517000cb003,0xd1a633d400000610,0x5b041014e5b95010,0x7f00011689280804,0x99052f69e49a21,0xa18dbea8217401e0,0x38a1060d4610b01,0x8878a78712601e11,0x8840618a30c0d81,0x21cc29d60c8a6d02,0x863822906a420808,0x20210680030e2040,0x4094182523302010,0x14d00000d84b9020,0x6ad30009e59013c4,0x41020822033021f5,0xe00003040408630,0x2082125380810002,0xa9c202200a00a01e,0xb0a0aac718030608,0x2166829a8106b122,0x41e40804cd00,0xa28e1950c1d0320d,0xc1695b92b0524108,0x6580c51d90890040,0x204240102083057,0x8431f1002d6111d0,0x11411c17401418,0xfa420830410e3710,0x9b2551b418804108,0x6a08a4830c10680,0xc08618f20204e044,0xc2c148324140004,0x21b0c30d64ee36d0,0x22c16400152c1183,0x400210001d420104,0xf80c00a1f14f0122,0x7a82a4028458a211,0xcd80c04f08304868,0x600058e1118a0077,0x821982a6d8058001,0x48f03600a1179016,0x600f2e3300004bb0,0x8b000ca04e18cf2,0xa86182102089006b,0xc3168840c4002285,0x8a80ee804080840b,0xc800000c20080840,0x21480009bf084bb4,0x89d80d8398001608,0x700c3098c5ea40e3,0x8a30d8140270001d,0xd29620411049329,0xec000000082ad010,0xa0304d1910414c03,0x188811e200cec0,0x860997800e49cb00,0x4570410c1c00009,0x3168001dc4041081,0xc404204c2460c519,0x830440c58413801,0xfd80000001081042,0x620004cd0c1d4249,0x20980000090810,0x1f0708310e1c1060,0x4000640000d40a4c,0x8120d08a001d40c,0x9cc025157484086,0x828085093f80010,0x8d4005e002a80008,0xa30283820281030,0xa0f8b01808083348,0xe01d43119108108,0x10b5b0703cb9d730,0x28082020481474e8,0xe002984878409810,0x804200c243df000f,0xc0044d0c00004880,0xc0005380850ac052,0xc44012900810790,0x25e41815640cc01,0x6438406c430501c9,0x430260a600c535d0,0xc1c0c34080c18181,0x28180c01341c4c0,0xc2038fa80fd800e7,0x35ca80001a701810,0x5160dd81ca4498,0x5f0220089290618,0x2940ca49f801168,0x262d8001180c730,0x80170e44c843e128,0x4083dd8a00101145,0x504300d540103041,0x2111a4193227801,0x201020614b041050,0x400188070c41406,0x4490059090ea8240,0x1ce60d7462c43d90,0x40000006d704414b,0xc114374118304e14,0x6f2c60014d19030,0x1153859cc01a08a,0x1d08528120128110,0x12481cb0034d49,0x20418012d40046b1,0x426d67961067df04,0x1410801b98c21091,0xba56244078143218,0x423c508605948719,0x5c0853000000e010,0x54e1251429000542,0x54800080572c37c,0x4c62811581040c21,0x8002321528080c13,0x68e6cea3239a119b,0x5910b80b24103400,0x473e07a10a0aec1,0xb822023022022b62,0x200a29039c233000,0x40aa5040940a608,0x9801b74000f58240,0xa98220229c283004,0x8011600209a18808,0x7fa164b0f5076051,0xe0010822240df76a,0x8fc411001021060f,0xa0606c862000784c,0x2190b3b280406102,0x1210819a10210c60,0xe007c5c9011b2b,0x303873940022120,0x430104a1d3f82076,0xa502e0011015d70e,0x149ccc5810812420,0x40032017809c3226,0xdf440e030660c255,0x15a0c20c60fc0065,0x41046041,0xe600044cdc20428c,0xd52000040000020,0x1f006d104f185001,0x4024364730d3c042,0x832435c6045701d,0x32067ce806082248,0xd1600381023d8b00,0x2012006c8e096031,0x8e0bcc044004c40,0x2a086b4480011530,0x5405952840a8a807,0x603048c1050a302,0xd581be70100066,0xa7c04f029c000053,0x980a8000824a0088,0xe0895241c10804e4,0x303b325059081a19,0x8506104061009688,0x302b18042193,0x42482821c0a90e54,0x289200b832247b80,0x6400481021bb8190,0xe8c00806050820c2,0x503f828708182841,0x971a4800002d039,0x60001048c0111310,0xa5013f0a6b48203,0xf7c4c8c872600512,0x81080b041831e0c0,0x7900260a541041e1,0xb00012040a22fe00,0x651160006357800,0x58101608d8028808,0x331c2e4e18560e,0x6c2006c65a167596,0x1202020612091965,0xe800a81022042c06,0x6040810560126003,0x61473000a3e58000,0x3030428860004040,0x72c072701046c014,0x603110a842004300,0x804021d499500043,0x6050b117cabd6501,0xc061502190103060,0xf40048504562b004,0x8106783080e3eb01,0x5d81ca4e22e00003,0x2a100a0118000,0x30c3844605515040,0xc8412082005c0bb0,0x40181e4000127c04,0x3428f840c1058213,0x840c580260020000,0x800607084160450b,0xda80d03984207e80,0xa670a5b600c50279,0x30000000428a1083,0x80de23c01eb00b04,0x8208939203e08c08,0xf90182502b803207,0x637a0620302bc023,0x54a9babc77a64a,0xaac68c19090c1200,0x221e08e135180c9e,0x900b40097c938160,0x2b3000380aae391,0xa214200048292252,0x889628c10208a48,0xcfc0b8059581c1ab,0xf260618614086f20,0x1042c08b8005bdc0,0x20204020ec53040c,0xb060084027820a08,0x840a06f871e3c2a0,0x81254601a2386040,0x808601403008f037,0xc780548606980001,0x682b40c029001810,0x517005229a21ba14,0xdc86012c202228c,0x901a24489066c383,0xf92009878208680,0x12400269e001c00e,0x4f9f0804ec0647a8,0x1020328020a098b8,0x20fe509ae0a2f802,0x2982000010088420,0x8242aa8c66e21bb6,0xd4021836004821,0x808214e082000000,0x8000138001410e57,0x84082c92a860006,0x825002bb80040383,0x90082782a0cd8004,0x30920e0852063823,0xa06000c1081ec580,0x94893056060a8e74,0x285819f11ac00602,0x421c5008b68a0001,0x8024081918108041,0x670f8012c410e04f,0x53a2c581340c2011,0x2043c08501140848,0x2a101b4000011604,0xc02d804a0060108,0x3006a20022441c0,0x28202041ad12440,0x10158c02608f,0x50a70914074862c,0x944a005819c13c40,0x9340230a10e59127,0x33c043a7460042d0,0x5000010c819e1c10,0x12001c0408a8e07,0x10b01b98c104bc,0x120c040d20165dce,0xc1dd009c21710044,0xb81c0c0607804057,0x116403588c34fbaa,0x64b4578dfba014e0,0x80813d001f4004bd,0x5d802b8002850287,0x480a117c580008e,0x488183820581b181,0x81e9000107844681,0x3ab808f00922080,0x43838400ccd55e85,0x1600038282291580,0x10786122070105a0,0x2800000660000c08,0x85085d5000001382,0x85832616a8280380,0x620b03120c3092c0,0x104300072a10a041,0xb4c0600000072c0a,0x1009c44091830c10,0x1404c00480300006,0xc806002029209812,0x4c5723a1818d061c,0x28c614071428802,0xa823a00003,0x1028882020ca,0x9e0a8a00f47090,0x802244880490001b,0xc8a2e00efc272448,0x1c11b02f04073424,0xc0e82060ca8144,0x8322050803c00ba4,0x5058060220360100,0x2f845010c3010828,0x30a806930744094,0xc185061cb0238000,0xa0c200041c272020,0x3000050724193898,0x3840cdc0aa3d4043,0xcd1c8c18e0454,0x5e02508028c103da,0x806042859618300,0x40260204a8d7b741,0x20df8a7a00b802cb,0x118e190a608c9805,0x903a0049ce3e241,0x608396045c000564,0xb422012800101830,0x230a1c09721184a,0xc891b08040b3033c,0x20cf9110b031529,0x9e02d8000022c32c,0x29001415502041,0xc00181332c198f54,0x706fc34700380176,0x4280184d83001012,0x238a04b6052256a2,0x59000b012e439005,0x1840cae28c40041a,0x40c210021f827e01,0x4081812a9c0a20c,0xe5000030b023828e,0x4028001c10c05240,0x6e25800f5c094df6,0x4e05f846e04c80c,0x1540c0408b400be0,0xc04642be88c44481,0x5642870500c47245,0x9e8c1bf88814107,0x80c600d480c18049,0x421300bd405ac040,0x9880c1b6a9828041,0x4fc0f6544ff3809c,0x9c0,0};
	int bitIndex = 0, sq = -1, wordSize, wordSizeOnes, frame, next, mod, div;
	
	int ReadNextWord(int wordSize) {
		mod = bitIndex % 64;
		div = bitIndex / 64;

		wordSizeOnes = (1 << wordSize) - 1;
		bitIndex += wordSize;

		return (int)(data[div] >> mod | (mod == 0 ? 0 : data[++div] << 64 - mod)) & wordSizeOnes;
	}
	
	public Move Think(Board board, Timer timer) {
		while (++sq < 64) {
			// Wordsize ranges from 4 to 4 + 2^3 - 1 = 11
			wordSize = ReadNextWord(3) + 4;
			frame = 0;

			while (frame < 3629) {
				frame += next = ReadNextWord(wordSize);
				if (next == 0)
					frame += wordSizeOnes;
				else
					diffs[frame] |= 1UL << sq;
			}
		}
		
		// Invert the first frame, so red correlates with black in the video:
		diffs[0] = ~0ul;

		// The variable next can be re-used, because the data gets compressed in a way such that it ends up having the value 0.
		while (++next < 3630) {
			BitboardHelper.VisualizeBitboard(diffs[0] ^= diffs[next]);
			while (timer.MillisecondsElapsedThisTurn < next * 33.333333) {}
		}
		
		// Even though the video takes twice as long as the time control allows for, it is a valid chess-bot ;)
		return board.GetLegalMoves()[0];
	}
}