namespace NFTMarketServer.Grains.Grain.ApplicationHandler;

public class OpenAiOptions
{
    public string ImagesUrlV1 { get; set; }
    public List<string> ApiKeyList { get; set; }
    public List<string> ApiKeyListTmp { get; set; }

    public int DelayMaxTime { get; set; } = 0;
    public int DelayMillisecond { get; set; } = 0;
    
    public bool RepeatRequestIsOn { get; set; } = false;
    
    public string WordCheckUrl { get; set; }

}

public class ChainOptions
{
    public Dictionary<string, ChainInfo> ChainInfos { get; set; }
}

public class SynchronizeTransactionJobOptions
{
    public int CrossChainDelay { get; set; } = 1000;
    public long RetryTimes { get; set; } = 5;
    public long BeginHeight { get; set; } = -1;
}

public class SynchronizeSeedJobOptions
{
    public string ToChainId { get; set; } 
}

public class HideCollectionInfoOptions
{
    public List<string> HideCollectionInfoList { get; set; }
}

public class RandomImageListOptions
{
    public List<string> RandomImageList { get; set; }
}

public class RarityShowWhiteOptions
{
    public List<string> RarityShowWhiteList { get; set; }
}

public class CollectionTradeInfoOptions
{
    public bool GrayIsOn { get; set; } = false;
    public List<string> CollectionIdList { get; set; } = new List<string>();
}

public class ResetNFTSyncHeightExpireMinutesOptions
{
    public int ResetNFTSyncHeightExpireMinutes { get; set; }
}

public class ChoiceNFTInfoNewFlagOptions
{
    public bool ChoiceNFTInfoNewFlagIsOn { get; set; }
}

public class CollectionActivityNFTLimitOptions
{
    public int CollectionActivityNFTLimit { get; set; } = 1000;
}

public class AIPromptsOptions
{
    public string AIPrompts { get; set; } = "sunrise/mountain/lake/forest/beach/cityscape/skyline/bridge/river/waterfall/desert/canyon/cliff/valley/hill/volcano/island/ocean/sea/pier/dock/sand/dune/wave/reef/coral/tree/bush/flower/grass/field/meadow/prairie/park/garden/trail/path/road/street/alley/building/skyscraper/tower/castle/fort/palace/temple/church/mosque/synagogue/pagoda/monastery/ruins/statue/monument/fountain/lighthouse/windmill/barn/farm/ranch/vineyard/orchard/market/bazaar/stall/tent/campfire/cabin/shack/hut/house/mansion/villa/bungalow/cottage/apartment/condo/loft/attic/basement/garage/shed/greenhouse/treehouse/playground/slide/swing/seesaw/merry-go-round/jungle gym/bench/picnic table/gazebo/pergola/pavilion/pool/pond/lake/ocean/sea/beach/island/boat/ship/sailboat/yacht/canoe/kayak/raft/ferry/cruise ship/submarine/airplane/helicopter/glider/hot air balloon/parachute/kite/bicycle/motorcycle/scooter/moped/skateboard/rollerblades/snowmobile/snowboard/skis/sled/dog sled/horse/cow/cat/dog/elephant/lion/tiger/bear/deer/fox/wolf/rabbit/squirrel/bird/eagle/hawk/owl/peacock/pigeon/sparrow/parrot/crow/raven/penguin/flamingo/heron/stork/duck/goose/swan/whale/dolphin/shark/octopus/jellyfish/crab/lobster/shrimp/fish/starfish/seahorse/urchin/snake/lizard/turtle/frog/crocodile/alligator/insect/butterfly/moth/beetle/ladybug/ant/bee/wasp/spider/scorpion/dragonfly/firefly/grasshopper/cricket/cockroach/locust/termite/fly/mosquito/worm/snail/slug/mushroom/toadstool/fern/moss/lichen/ivy/vine/rose/tulip/daisy/sunflower/orchid/lily/daffodil/iris/peony/magnolia/jasmine/lavender/dandelion/clover/poppy/lotus/bamboo/pine/oak/maple/willow/cherry/apple/orange/banana/grape/strawberry/blueberry/raspberry/blackberry/watermelon/cantaloupe/honeydew/kiwi/papaya/mango/pear/peach/plum/cherry/fig/date/coconut/pomegranate/persimmon/apricot/avocado/guava/lychee/passion fruit/dragon fruit/starfruit/rambutan/pumpkin/squash/zucchini/cucumber/tomato/potato/carrot/beet/onion/garlic/ginger/pepper/chili/eggplant/lettuce/spinach/kale/chard/cabbage/broccoli/cauliflower/radish/turnip/artichoke/asparagus/celery/fennel/dill/basil/thyme/rosemary/oregano/parsley/cilantro/mint/sage/tarragon/lemongrass/bay leaf/curry leaf/nutmeg/cinnamon/clove/cardamom/cumin/coriander/peppercorn/saffron/turmeric/paprika/chili powder/cayenne/jalapeno/serrano/habanero/ghost pepper/carolina reaper/banana pepper/poblano/ancho/guajillo/pasilla/chipotle/mole/salsa/guacamole/hummus/tzatziki/tahini/salad/dressing/marinade/vinaigrette/mustard/ketchup/mayonnaise/soy sauce/teriyaki/worcestershire/barbecue/buffalo/hot sauce/tabasco/sriracha/chili sauce/sweet and sour/duck sauce/plum sauce/hoisin/oyster/fish sauce/tamari/miso/wasabi/ponzu/yuzu/kombu/nori/wakame/hijiki/arame/sushi/sashimi/nigiri/maki/uramaki/temaki/onigiri/donburi/ramen/udon/soba/sukiyaki/shabu-shabu/tempura/teriyaki/yakitori/teppanyaki/okonomiyaki/takoyaki/miso soup/tonkatsu/katsu curry/karaage/goya/gyoza/yakiniku/takikomi gohan/ochazuke/onabe/zoni/omurice/chahan/edamame/tamago/sukiyaki/yosenabe/oden/korokke/kakigori/matcha/matcha latte/sencha/genmaicha/hojicha/gyokuro/kabusecha/bancha/kukicha/kombucha/sencha/fukamushi/konacha/mecha/kokucha/shincha/sakuracha/awancha/iwakeya/sapporo/kirin/asahi/suntory/yebisu/sapporo classic/sapporo premium/sapporo reserve/kirin ichiban/kirin light/kirin free/kirin stout/kirin zero/asa/yebisu premium/yebisu black/yebisu creamy/top/draft/yebisu seasonal/yebisu vintage/yebisu royal/yebisu spring/yebisu summer/yebisu autumn/yebisu winter/yebisu silk/yebisu double/yebisu floral/yebisu amber/yebisu pale/yebisu dark/yebisu wheat/yebisu rice/yebisu barley/yebisu corn/yebisu oats/yebisu millet/yebisu sorghum/yebisu rye/yebisu buckwheat/yebisu quinoa/yebisu chia/yebisu flax/yebisu hemp/yebisu sunflower/yebisu sesame/yebisu pumpkin/yebisu watermelon/yebisu grape/yebisu cherry/yebisu apple/yebisu peach/yebisu plum/yebisu apricot/yebisu pear/yebisu fig/yebisu date/yebisu olive/yebisu coconut/yebisu avocado/yebisu persimmon/yebisu pomegranate/yebisu kiwi/yebisu papaya/yebisu guava/yebisu lychee/yebisu passion fruit/yebisu dragon fruit/yebisu starfruit/yebisu rambutan/yebisu mangosteen/yebisu jackfruit/yebisu durian/yebisu dragon's eye/yebisu longan/yebisu sugar apple/yebisu custard apple/yebisu soursop/yebisu cherimoya/yebisu lucuma/yebisu camu camu/yebisu acai/yebisu goji/yebisu jujube/yebisu medlar/yebisu loquat/yebisu tamarind/yebisu carambola/yebisu miracle fruit/yebisu noni/yebisu bilberry/yebisu huckleberry/yebisu mulberry/yebisu elderberry/yebisu currant/yebisu gooseberry/yebisu cranberry/yebisu lingonberry/yebisu cloudberry/yebisu boysenberry/yebisu marionberry/yebisu tayberry/yebisu loganberry/yebisu dewberry/yebisu juneberry/yebisu thimbleberry/yebisu salmonberry/yebisu seagrape/yebisu beach plum/yebisu sand cherry/yebisu serviceberry/yebisu chokeberry/yebisu hackberry/yebisu mulberry/yebisu waxberry/yebisu bayberry/yebisu snowberry/yebisu buffalo berry/yebisu juniper/yebisu sea buckthorn/yebisu pin cherry/yebisu dogwood/yebisu redbud/yebisu magnolia/yebisu tulip tree/yebisu mimosa/yebisu silk tree/yebisu locust/yebisu acacia/yebisu black locust/yebisu honey locust/yebisu yellow locust/yebisu red locust/yebisu pink locust/yebisu purple locust/yebisu white locust/yebisu green locust/yebisu blue locust/yebisu golden locust/yebisu silver locust/yebisu bronze locust/yebisu platinum locust/yebisu diamond locust/yebisu ruby locust/yebisu emerald locust/yebisu sapphire locust/yebisu topaz locust/yebisu opal locust/yebisu amethyst locust/yebisu garnet locust/yebisu peridot locust/yebisu turquoise locust/yebisu lapis locust/yebisu onyx locust/yebisu jasper locust/yebisu jade locust/yebisu malachite locust/yebisu coral locust/yebisu amber locust/yebisu jet locust/yebisu agate locust/yebisu carnelian locust/yebisu chalcedony locust/yebisu bloodstone locust/yebisu chrysoprase locust/yebisu zircon locust/yebisu moonstone locust/yebisu sunstone locust/yebisu kyanite locust/yebisu beryl locust/yebisu tourmaline locust/yebisu fluorite locust/yebisu rhodochrosite locust/yebisu sphalerite locust/yebisu smithsonite";
}

public class StatisticsUserListRecordOptions
{
    public bool StatisticsSwitch { get; set; } = false;
    public bool SendTxSwitch { get; set; } = false;

}

public class PlatformNFTOptions
{
    public bool CreateSwitch { get; set; }
    public string CollectionSymbol { get; set; }
    
    public string SymbolPrefix { get; set; }
    public string CreateChainId { get; set; }
    public string CollectionOwnerAddress { get; set; }
    public string PrivateKey { get; set; }
    public string CollectionOwnerProxyAccountHash { get; set; }
    public string CollectionOwnerProxyAddress { get; set; }
    public string CollectionIssuerProxyAccountHash { get; set; }
    public string CollectionIssuerProxyAddress { get; set; }

    public int UserCreateLimit { get; set; } = 1;
    public string ProxyContractMainChainAddress { get; set; }
    public string ProxyContractSideChainAddress { get; set; }
    public string CollectionIcon{ get; set; }
    public string CollectionName{ get; set; }
}