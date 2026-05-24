import LiquidChrome from "./LiquidChrome";


function Background() {
    return (
        <div className="liquid-chrome-background">
            <LiquidChrome
                baseColor={[0.4, 0.5, 0.9]}
                speed={0.5}
                amplitude={0.6}
                frequencyX={3}
                frequencyY={3}
                interactive={false}
            />
        </div>
    );
}

export default Background;