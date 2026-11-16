import { testService } from '../services/testing/fake-services.service';
import { testSitePrice } from '../site-prices/testing/fake-site-prices.service';
import { resolveRate } from './price-resolver';

describe('resolveRate', () => {
    const services = [
        testService({ id: 'sv1', name: 'Боядисване', defaultRate: 6 }),
        testService({ id: 'sv2', name: 'Шпакловка', defaultRate: null }),
        testService({ id: 'sv3', name: 'Грундиране', defaultRate: 2.5 }),
    ];
    const sitePrices = [
        testSitePrice({ id: 'p1', serviceId: 'sv1', rate: 6.5 }),
        testSitePrice({ id: 'p2', serviceId: 'sv2', rate: 8 }),
    ];

    it('takes the price for the site first', () => {
        expect(resolveRate('sv1', sitePrices, services)).toBe(6.5);
        expect(resolveRate('sv2', sitePrices, services)).toBe(8);
    });

    it('falls back to the default price of the service', () => {
        expect(resolveRate('sv3', sitePrices, services)).toBe(2.5);
    });

    it('is null without a site price and a default price', () => {
        expect(resolveRate('sv2', [], services)).toBeNull();
    });

    it('is null for free text and for an unknown service', () => {
        expect(resolveRate(null, sitePrices, services)).toBeNull();
        expect(resolveRate('', sitePrices, services)).toBeNull();
        expect(resolveRate('missing', sitePrices, services)).toBeNull();
    });
});
