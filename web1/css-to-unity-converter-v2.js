#!/usr/bin/env node

/**
 * CSS to Unity USS Converter Tool v2.0
 * Improved parsing and conversion accuracy
 */

const fs = require('fs');
const path = require('path');

class CSSToUSSConverterV2 {
    constructor() {
        this.conversionRules = {
            // Direct property mappings
            'background-color': 'background-color',
            'color': 'color',
            'opacity': 'opacity',
            'width': 'width',
            'height': 'height',
            'min-width': 'min-width',
            'max-width': 'max-width',
            'min-height': 'min-height',
            'max-height': 'max-height',
            'font-size': 'font-size',
            'font-weight': 'font-weight',
            'font-family': 'font-family',

            // Custom conversion functions
            'background': this.convertBackground.bind(this),
            'border-radius': this.convertBorderRadius.bind(this),
            'transform': this.convertTransform.bind(this),
            'transition': this.convertTransition.bind(this),
            'box-shadow': this.convertBoxShadow.bind(this),
            'display': this.convertDisplay.bind(this),
            'position': this.convertPosition.bind(this),
            'padding': this.convertPadding.bind(this),
            'margin': this.convertMargin.bind(this),
            'border': this.convertBorder.bind(this),
            'flex-direction': this.convertFlexDirection.bind(this),
            'justify-content': this.convertJustifyContent.bind(this),
            'align-items': this.convertAlignItems.bind(this),
            'gap': this.convertGap.bind(this)
        };

        this.reportData = {
            converted: 0,
            warnings: [],
            unsupported: [],
            manualReview: []
        };
    }

    convertCSS(cssContent) {
        console.log('🎨 Converting CSS to USS (v2.0)...');

        // Remove comments first
        cssContent = this.removeComments(cssContent);

        // Parse CSS rules more accurately
        const rules = this.parseCSS(cssContent);
        let ussContent = '/* Converted from CSS to USS */\n\n';

        for (const rule of rules) {
            const convertedRule = this.convertRule(rule);
            if (convertedRule.trim()) {
                ussContent += convertedRule + '\n\n';
            }
        }

        return {
            content: ussContent,
            report: this.reportData
        };
    }

    removeComments(cssContent) {
        // Remove /* ... */ comments
        return cssContent.replace(/\/\*[\s\S]*?\*\//g, '');
    }

    parseCSS(cssContent) {
        const rules = [];

        // Improved regex for CSS rules
        const ruleRegex = /([^{}]+)\s*{\s*([^{}]*)\s*}/g;
        let match;

        while ((match = ruleRegex.exec(cssContent)) !== null) {
            const selector = match[1].trim();
            const declarations = match[2].trim();

            // Skip empty rules
            if (!declarations) continue;

            // Skip @keyframes and @media rules for basic conversion
            if (selector.startsWith('@')) {
                this.reportData.manualReview.push(`CSS at-rule skipped: ${selector}`);
                continue;
            }

            rules.push({
                selector: selector,
                declarations: this.parseDeclarations(declarations)
            });
        }

        return rules;
    }

    parseDeclarations(declarationBlock) {
        const declarations = [];

        // Split by semicolon and clean up
        const lines = declarationBlock.split(';');

        for (const line of lines) {
            const trimmed = line.trim();
            if (!trimmed) continue;

            const colonIndex = trimmed.indexOf(':');
            if (colonIndex === -1) continue;

            const property = trimmed.substring(0, colonIndex).trim();
            const value = trimmed.substring(colonIndex + 1).trim();

            if (property && value) {
                declarations.push({ property, value });
            }
        }

        return declarations;
    }

    convertRule(rule) {
        const convertedSelector = this.convertSelector(rule.selector);

        // Skip invalid selectors
        if (!convertedSelector) {
            this.reportData.manualReview.push(`Skipped invalid selector: ${rule.selector}`);
            return '';
        }

        let ussRule = `${convertedSelector} {\n`;
        let hasValidDeclarations = false;

        for (const declaration of rule.declarations) {
            const converted = this.convertDeclaration(declaration);
            if (converted) {
                ussRule += `    ${converted}\n`;
                hasValidDeclarations = true;
                this.reportData.converted++;
            }
        }

        ussRule += '}';

        // Only return rule if it has valid declarations
        return hasValidDeclarations ? ussRule : '';
    }

    convertSelector(selector) {
        // Clean up selector
        selector = selector.replace(/\s+/g, ' ').trim();

        // Skip pseudo-elements that USS doesn't support well
        if (selector.includes('::before') || selector.includes('::after')) {
            return null; // Skip these rules
        }

        // Convert basic selectors
        return selector
            .replace(/:hover/g, ':hover')
            .replace(/:active/g, ':active')
            .replace(/:focus/g, ':focus')
            .replace(/:disabled/g, ':disabled');
    }

    convertDeclaration(declaration) {
        const { property, value } = declaration;

        // Skip CSS variables and unknown properties
        if (property.startsWith('--') || value.includes('var(')) {
            this.reportData.unsupported.push(`CSS variable: ${property}: ${value}`);
            return null;
        }

        // Skip animation properties (need manual C# implementation)
        if (property.startsWith('animation') || property === 'will-change' ||
            property === 'backface-visibility' || property === 'perspective') {
            this.reportData.manualReview.push(`Animation property: ${property}: ${value}`);
            return null;
        }

        const rule = this.conversionRules[property];

        if (typeof rule === 'function') {
            return rule(value, property);
        } else if (typeof rule === 'string') {
            return `${rule}: ${value};`;
        } else {
            this.reportData.unsupported.push(`${property}: ${value}`);
            return `/* UNSUPPORTED: ${property}: ${value}; */`;
        }
    }

    // Improved conversion functions
    convertBackground(value) {
        if (value.includes('linear-gradient')) {
            this.reportData.manualReview.push(`Linear gradient needs Unity gradient setup: ${value}`);
            return `/* TODO: Setup Unity gradient for: ${value} */\n    background-color: rgba(99, 102, 241, 1);`;
        }
        if (value.includes('radial-gradient')) {
            this.reportData.manualReview.push(`Radial gradient not supported: ${value}`);
            return `/* TODO: Replace with solid color or texture */\n    background-color: rgba(99, 102, 241, 1);`;
        }
        return `background-color: ${value};`;
    }

    convertBorderRadius(value) {
        // Parse different border-radius formats
        const values = value.split(' ').map(v => v.trim());

        if (values.length === 1) {
            // All corners same
            const radius = values[0];
            return [
                `border-top-left-radius: ${radius};`,
                `border-top-right-radius: ${radius};`,
                `border-bottom-left-radius: ${radius};`,
                `border-bottom-right-radius: ${radius};`
            ].join('\n    ');
        } else if (values.length === 4) {
            // Top-left, top-right, bottom-right, bottom-left
            return [
                `border-top-left-radius: ${values[0]};`,
                `border-top-right-radius: ${values[1]};`,
                `border-bottom-right-radius: ${values[2]};`,
                `border-bottom-left-radius: ${values[3]};`
            ].join('\n    ');
        } else {
            // Fallback
            return `/* Complex border-radius: ${value} - Convert manually */`;
        }
    }

    convertTransform(value) {
        // Handle simple transforms
        if (value === 'none') {
            return 'transform-origin: center;';
        }

        // Extract translate values
        const translateMatch = value.match(/translate(?:X|Y)?\(([^)]+)\)/);
        if (translateMatch) {
            const translateValue = translateMatch[1];
            if (value.includes('translateX')) {
                return `translate: ${translateValue} 0;`;
            } else if (value.includes('translateY')) {
                return `translate: 0 ${translateValue};`;
            } else {
                const values = translateValue.split(',').map(v => v.trim());
                return `translate: ${values[0]} ${values[1] || '0'};`;
            }
        }

        // Extract scale values
        const scaleMatch = value.match(/scale\(([^)]+)\)/);
        if (scaleMatch) {
            const scaleValue = scaleMatch[1];
            return `scale: ${scaleValue} ${scaleValue};`;
        }

        // Extract rotate values
        const rotateMatch = value.match(/rotate\(([^)]+)\)/);
        if (rotateMatch) {
            return `rotate: ${rotateMatch[1]};`;
        }

        // Complex transform - needs manual conversion
        this.reportData.manualReview.push(`Complex transform: ${value}`);
        return `/* TODO: Convert complex transform: ${value} */`;
    }

    convertTransition(value) {
        const parts = value.split(' ').map(p => p.trim());
        const property = parts[0] || 'all';
        const duration = parts[1] || '0.3s';
        const easing = parts[2] || 'ease';

        return [
            `transition-property: ${property};`,
            `transition-duration: ${duration};`,
            `transition-timing-function: ${easing};`
        ].join('\n    ');
    }

    convertBoxShadow(value) {
        this.reportData.manualReview.push(`Box-shadow not supported, use border: ${value}`);
        return `/* TODO: Replace box-shadow with border: ${value} */`;
    }

    convertDisplay(value) {
        const displayMap = {
            'flex': 'flex',
            'none': 'none',
            'block': 'flex',
            'inline': 'flex',
            'inline-block': 'flex'
        };
        return `display: ${displayMap[value] || value};`;
    }

    convertPosition(value) {
        const positionMap = {
            'absolute': 'absolute',
            'relative': 'relative',
            'fixed': 'absolute', // USS doesn't have fixed
            'static': 'relative'
        };
        return `position: ${positionMap[value] || value};`;
    }

    convertPadding(value) {
        return this.convertSpacing(value, 'padding');
    }

    convertMargin(value) {
        return this.convertSpacing(value, 'margin');
    }

    convertSpacing(value, type) {
        const values = value.split(' ').map(v => v.trim());

        if (values.length === 1) {
            // All sides same
            return [
                `${type}-top: ${value};`,
                `${type}-right: ${value};`,
                `${type}-bottom: ${value};`,
                `${type}-left: ${value};`
            ].join('\n    ');
        } else if (values.length === 2) {
            // Top/bottom, left/right
            return [
                `${type}-top: ${values[0]};`,
                `${type}-right: ${values[1]};`,
                `${type}-bottom: ${values[0]};`,
                `${type}-left: ${values[1]};`
            ].join('\n    ');
        } else if (values.length === 4) {
            // Top, right, bottom, left
            return [
                `${type}-top: ${values[0]};`,
                `${type}-right: ${values[1]};`,
                `${type}-bottom: ${values[2]};`,
                `${type}-left: ${values[3]};`
            ].join('\n    ');
        }

        return `${type}: ${value};`;
    }

    convertBorder(value) {
        // Simple border conversion
        if (value === 'none' || value === '0') {
            return 'border-width: 0;';
        }

        const parts = value.split(' ');
        if (parts.length >= 3) {
            return [
                `border-width: ${parts[0]};`,
                `border-color: ${parts[2]};`
            ].join('\n    ');
        }

        return `/* TODO: Convert border manually: ${value} */`;
    }

    convertFlexDirection(value) {
        return `flex-direction: ${value};`;
    }

    convertJustifyContent(value) {
        const mappings = {
            'center': 'center',
            'flex-start': 'flex-start',
            'flex-end': 'flex-end',
            'space-between': 'space-between',
            'space-around': 'space-around'
        };
        return `justify-content: ${mappings[value] || value};`;
    }

    convertAlignItems(value) {
        const mappings = {
            'center': 'center',
            'flex-start': 'flex-start',
            'flex-end': 'flex-end',
            'stretch': 'stretch'
        };
        return `align-items: ${mappings[value] || value};`;
    }

    convertGap(value) {
        // USS uses margin for gap simulation
        this.reportData.manualReview.push(`Gap property - use margin instead: ${value}`);
        return `/* TODO: Use margin to simulate gap: ${value} */`;
    }

    generateReport() {
        console.log('\n📊 Enhanced Conversion Report (v2.0):');
        console.log(`✅ Converted: ${this.reportData.converted} properties`);
        console.log(`⚠️  Manual review: ${this.reportData.manualReview.length} items`);
        console.log(`❌ Unsupported: ${this.reportData.unsupported.length} properties`);

        if (this.reportData.manualReview.length > 0) {
            console.log('\n⚠️  Manual Review Required:');
            this.reportData.manualReview.slice(0, 10).forEach(item => console.log(`   - ${item}`));
            if (this.reportData.manualReview.length > 10) {
                console.log(`   ... and ${this.reportData.manualReview.length - 10} more items`);
            }
        }

        // Conversion tips
        console.log('\n💡 Conversion Tips:');
        console.log('   - Gradients → Use Unity Gradient component');
        console.log('   - Box shadows → Use borders or background textures');
        console.log('   - Complex animations → Implement in C# with Unity animation');
        console.log('   - CSS Grid → Convert to flexbox layout');
    }
}

// Usage
function convertProjectV2(inputDir, outputDir) {
    const converter = new CSSToUSSConverterV2();

    console.log('🚀 Starting Enhanced CSS to Unity conversion (v2.0)...\n');

    if (!fs.existsSync(outputDir)) {
        fs.mkdirSync(outputDir, { recursive: true });
    }

    const cssFiles = fs.readdirSync(inputDir).filter(file => file.endsWith('.css'));

    for (const cssFile of cssFiles) {
        console.log(`📄 Converting ${cssFile} (enhanced)...`);

        const cssPath = path.join(inputDir, cssFile);
        const cssContent = fs.readFileSync(cssPath, 'utf8');

        const result = converter.convertCSS(cssContent);

        const ussFileName = cssFile.replace('.css', '-v2.uss');
        const outputPath = path.join(outputDir, ussFileName);

        fs.writeFileSync(outputPath, result.content);
        console.log(`✅ Created ${ussFileName}`);
    }

    converter.generateReport();
    console.log(`\n🎉 Enhanced conversion complete! Check ${outputDir} for results.`);
}

// CLI usage
if (require.main === module) {
    const args = process.argv.slice(2);
    const inputDir = args[0] || './css';
    const outputDir = args[1] || './unity-ui-v2';

    if (!fs.existsSync(inputDir)) {
        console.error(`❌ Input directory not found: ${inputDir}`);
        process.exit(1);
    }

    convertProjectV2(inputDir, outputDir);
}

module.exports = { CSSToUSSConverterV2, convertProjectV2 }; 